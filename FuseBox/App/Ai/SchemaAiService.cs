using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FuseBox.App.SchemaEditor;
using Microsoft.Extensions.Options;

namespace FuseBox.Ai;

public sealed class SchemaAiService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiConfiguratorOptions _options;

    public SchemaAiService(HttpClient httpClient, IOptions<OpenAiConfiguratorOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SchemaChatResponse> ChatAsync(SchemaChatRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var apiKey = ResolveApiKey();
        var model = ResolveModel();
        var maxRounds = Math.Clamp(_options.MaxToolRounds, 1, 12);
        var maxOutputTokens = Math.Clamp(_options.MaxOutputTokens, 256, 8000);
        var input = BuildInitialInput(request);
        var commands = new List<JsonObject>();
        string? finalMessage = null;

        for (var round = 0; round < maxRounds; round++)
        {
            var payload = new JsonObject
            {
                ["model"] = model,
                ["input"] = input.DeepClone(),
                ["tools"] = SchemaAiTools.CreateAll(),
                ["tool_choice"] = "auto",
                ["parallel_tool_calls"] = false,
                ["max_output_tokens"] = maxOutputTokens,
                ["store"] = false,
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "responses");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(BuildOpenAiError(responseText, (int)response.StatusCode));

            var root = JsonNode.Parse(responseText) as JsonObject
                ?? throw new InvalidOperationException("OpenAI returned an invalid JSON response.");
            var output = root["output"] as JsonArray
                ?? throw new InvalidOperationException("OpenAI response is missing output items.");
            var hadToolCalls = false;

            foreach (var itemNode in output)
            {
                if (itemNode is not JsonObject item) continue;
                input.Add(item.DeepClone());

                if (item["type"]?.GetValue<string>() != "function_call") continue;
                hadToolCalls = true;

                var callId = RequireNodeString(item, "call_id");
                var functionName = RequireNodeString(item, "name");
                var argumentText = RequireNodeString(item, "arguments");
                var arguments = JsonNode.Parse(argumentText) as JsonObject
                    ?? throw new InvalidOperationException($"Tool {functionName} returned invalid arguments.");
                var command = SchemaAiCommandFactory.Create(functionName, arguments);
                commands.Add(command);

                var toolOutput = new JsonObject
                {
                    ["accepted"] = true,
                    ["queued"] = true,
                    ["command"] = command.DeepClone(),
                    ["note"] = "The frontend will validate and apply this through the normal editor operation reducer after this turn. Nothing is saved automatically.",
                };

                input.Add(new JsonObject
                {
                    ["type"] = "function_call_output",
                    ["call_id"] = callId,
                    ["output"] = toolOutput.ToJsonString(),
                });
            }

            if (!hadToolCalls)
            {
                finalMessage = ExtractOutputText(output);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(finalMessage))
        {
            finalMessage = commands.Count > 0
                ? "I prepared schema edits in the local draft. Review them and press Save schema when you are ready."
                : "I could not produce a complete schema edit for this request.";
        }

        return new SchemaChatResponse(finalMessage.Trim(), commands);
    }

    private string ResolveApiKey()
    {
        var configured = _options.ApiKey?.Trim();
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var environment = Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Trim();
        if (!string.IsNullOrWhiteSpace(environment)) return environment;
        throw new InvalidOperationException("OpenAI API key is not configured. Set OPENAI_API_KEY or OpenAI:Configurator:ApiKey.");
    }

    private string ResolveModel()
    {
        var model = _options.Model?.Trim();
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("OpenAI:Configurator:Model is empty.");
        return model;
    }

    private static void ValidateRequest(SchemaChatRequest request)
    {
        if (request.Messages is null || request.Messages.Count == 0)
            throw new ArgumentException("At least one chat message is required.");
        if (request.Messages.Count > 24)
            throw new ArgumentException("Chat history is too long.");

        foreach (var message in request.Messages)
        {
            if (message.Role is not ("user" or "assistant"))
                throw new ArgumentException("Chat message role must be user or assistant.");
            if (string.IsNullOrWhiteSpace(message.Content) || message.Content.Length > 6000)
                throw new ArgumentException("Chat message content is invalid.");
        }

        if (request.Snapshot.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Schema snapshot must be a JSON object.");
    }

    private static JsonArray BuildInitialInput(SchemaChatRequest request)
    {
        var input = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "developer",
                ["content"] = BuildDeveloperPrompt(request.Snapshot),
            },
        };

        foreach (var message in request.Messages)
        {
            input.Add(new JsonObject
            {
                ["role"] = message.Role,
                ["content"] = message.Content.Trim(),
            });
        }

        return input;
    }

    private static string BuildDeveloperPrompt(JsonElement snapshot)
    {
        var snapshotJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });
        var catalog = ComponentCatalog.Entries
            .Where(entry => entry.Mounting == "din" && entry.Slots > 0 && entry.Role != "spacer")
            .Select(entry => new
            {
                id = entry.Id,
                role = entry.Role,
                slots = entry.Slots,
                terminalModel = entry.TerminalModel,
                terminals = entry.Terminals.Select(terminal => new
                {
                    id = terminal.Id,
                    type = terminal.Type,
                    conductor = terminal.Conductor,
                    phaseSelectable = terminal.PhaseSelectable,
                })
            });
        var catalogJson = JsonSerializer.Serialize(catalog);

        return $$"""
You are the AI assistant embedded in the FuseBox manual electrical-panel schema editor.

Your job is to answer questions about the CURRENT LOCAL SCHEMA DRAFT and, when asked, queue edits using the provided schema tools.

Hard rules:
- Respond in the same language as the user's latest message.
- Use the exact stable component IDs, connection IDs and terminal IDs from the snapshot.
- AI edits are applied by the React frontend through the SAME EditorOperation reducer used by human actions. The frontend will reject invalid placement or electrical operations.
- Tool calls modify only the LOCAL DRAFT. Never claim that the schema was saved, persisted, priced, or regenerated. The user must explicitly press Save schema.
- Do not bypass validation, invent database IDs, invent SchemaId values, or invent terminal IDs.
- DIN positions are logical rowIndex/slotStart values. Respect snapshot.geometry. Components may not overlap or extend beyond the rail.
- For add/replace, use catalogTypeId values from the catalog below. MCB ratings supported by the editor palette are 2,4,6,10,16,20,25,32 A. RCD sensitivities are 10,30,100,300,500 mA.
- New phase-selectable protection devices default to L1 when phase is omitted. Use set_schema_component_phase when the user explicitly asks for another phase.
- Do not silently cascade-delete wires or dependent components when removing a component. Invalid intermediate drafts are allowed; backend validation will block Save until fixed.
- For connect_terminals, source must be output/universal, target input/universal, phases/conductors must match, target inputs cannot be occupied, and duplicates/self-connections are invalid.
- Prefer small, targeted edits. Preserve all unspecified existing components, positions and connections.
- After tool calls, briefly summarize what was queued and remind the user that Save schema is still required.
- Do not output raw tool JSON.

PLACEABLE COMPONENT CATALOG JSON:
{{catalogJson}}

CURRENT LOCAL SCHEMA SNAPSHOT JSON:
{{snapshotJson}}
""";
    }

    private static string ExtractOutputText(JsonArray output)
    {
        var parts = new List<string>();
        foreach (var itemNode in output)
        {
            if (itemNode is not JsonObject item || item["type"]?.GetValue<string>() != "message" || item["content"] is not JsonArray content)
                continue;
            foreach (var contentNode in content)
            {
                if (contentNode is JsonObject contentItem && contentItem["type"]?.GetValue<string>() == "output_text" && contentItem["text"] is JsonValue textValue && textValue.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
                    parts.Add(text.Trim());
            }
        }
        return string.Join("\n\n", parts);
    }

    private static string RequireNodeString(JsonObject value, string key)
    {
        if (value[key] is JsonValue node && node.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
            return text;
        throw new InvalidOperationException($"OpenAI function call is missing {key}.");
    }

    private static string BuildOpenAiError(string responseText, int statusCode)
    {
        try
        {
            var root = JsonNode.Parse(responseText) as JsonObject;
            var message = root?["error"]?["message"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(message)) return $"OpenAI API error ({statusCode}): {message}";
        }
        catch { }
        return $"OpenAI API request failed with HTTP {statusCode}.";
    }
}
