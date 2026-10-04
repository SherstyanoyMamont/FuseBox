using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace FuseBox.Ai;

public sealed class ConfiguratorAiService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiConfiguratorOptions _options;

    public ConfiguratorAiService(
        HttpClient httpClient,
        IOptions<OpenAiConfiguratorOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<ConfiguratorChatResponse> ChatAsync(
        ConfiguratorChatRequest request,
        CancellationToken cancellationToken)
    {
        return ChatAsync(
            request,
            Array.Empty<ConfiguratorChatAttachment>(),
            cancellationToken
        );
    }

    public async Task<ConfiguratorChatResponse> ChatAsync(
        ConfiguratorChatRequest request,
        IReadOnlyList<ConfiguratorChatAttachment> attachments,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        ValidateAttachments(attachments);

        var apiKey = ResolveApiKey();
        var model = ResolveModel();
        var maxRounds = Math.Clamp(_options.MaxToolRounds, 1, 12);
        var maxOutputTokens = Math.Clamp(_options.MaxOutputTokens, 256, 8000);

        var input = BuildInitialInput(request, attachments);
        var commands = new List<JsonObject>();
        string? finalMessage = null;

        for (var round = 0; round < maxRounds; round++)
        {
            var payload = new JsonObject
            {
                ["model"] = model,
                // JsonNode instances can only have one parent. The same input
                // history is reused across tool rounds, so attach a deep clone
                // to each request payload instead of re-parenting the original.
                ["input"] = input.DeepClone(),
                ["tools"] = ConfiguratorAiTools.CreateAll(),
                ["tool_choice"] = "auto",
                ["parallel_tool_calls"] = true,
                ["max_output_tokens"] = maxOutputTokens,
                ["store"] = false,
            };

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "responses"
            );

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            httpRequest.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            var responseText = await response.Content.ReadAsStringAsync(
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    BuildOpenAiError(responseText, (int)response.StatusCode)
                );
            }

            var root = JsonNode.Parse(responseText) as JsonObject
                ?? throw new InvalidOperationException(
                    "OpenAI returned an invalid JSON response."
                );

            var output = root["output"] as JsonArray
                ?? throw new InvalidOperationException(
                    "OpenAI response is missing output items."
                );

            var hadToolCalls = false;

            foreach (var itemNode in output)
            {
                if (itemNode is not JsonObject item)
                {
                    continue;
                }

                input.Add(item.DeepClone());

                var type = item["type"]?.GetValue<string>();

                if (type == "function_call")
                {
                    hadToolCalls = true;

                    var callId = RequireNodeString(item, "call_id");
                    var functionName = RequireNodeString(item, "name");
                    var argumentText = RequireNodeString(item, "arguments");

                    var arguments = JsonNode.Parse(argumentText) as JsonObject
                        ?? throw new InvalidOperationException(
                            $"Tool {functionName} returned invalid arguments."
                        );

                    var command = ConfiguratorAiCommandFactory.Create(
                        functionName,
                        arguments
                    );

                    commands.Add(command);

                    input.Add(new JsonObject
                    {
                        ["type"] = "function_call_output",
                        ["call_id"] = callId,
                        ["output"] = "{\"accepted\":true,\"queued\":true,\"note\":\"The frontend will apply this command after the assistant finishes this turn.\"}",
                    });
                }
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
                ? "I prepared the requested configurator changes. Review them in the interface before saving."
                : "I could not produce a complete answer for this request.";
        }

        return new ConfiguratorChatResponse(
            finalMessage.Trim(),
            commands
        );
    }

    private string ResolveApiKey()
    {
        var configured = _options.ApiKey?.Trim();

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var environment = Environment
            .GetEnvironmentVariable("OPENAI_API_KEY")
            ?.Trim();

        if (!string.IsNullOrWhiteSpace(environment))
        {
            return environment;
        }

        throw new InvalidOperationException(
            "OpenAI API key is not configured. Set OPENAI_API_KEY or OpenAI:Configurator:ApiKey."
        );
    }

    private string ResolveModel()
    {
        var model = _options.Model?.Trim();

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                "OpenAI:Configurator:Model is empty."
            );
        }

        return model;
    }

    private static void ValidateRequest(
        ConfiguratorChatRequest request)
    {
        if (request.Messages is null || request.Messages.Count == 0)
        {
            throw new ArgumentException(
                "At least one chat message is required."
            );
        }

        if (request.Messages.Count > 24)
        {
            throw new ArgumentException(
                "Chat history is too long."
            );
        }

        foreach (var message in request.Messages)
        {
            if (message.Role is not ("user" or "assistant"))
            {
                throw new ArgumentException(
                    "Chat message role must be user or assistant."
                );
            }

            if (string.IsNullOrWhiteSpace(message.Content) ||
                message.Content.Length > 6000)
            {
                throw new ArgumentException(
                    "Chat message content is invalid."
                );
            }
        }

        if (request.Snapshot.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                "Configurator snapshot must be a JSON object."
            );
        }
    }

    private static JsonArray BuildInitialInput(
        ConfiguratorChatRequest request,
        IReadOnlyList<ConfiguratorChatAttachment> attachments)
    {
        var input = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "developer",
                ["content"] = BuildDeveloperPrompt(request.Snapshot),
            },
        };

        for (var index = 0; index < request.Messages.Count; index++)
        {
            var message = request.Messages[index];
            var isLastUserMessage =
                index == request.Messages.Count - 1 &&
                message.Role == "user";

            if (isLastUserMessage && attachments.Count > 0)
            {
                var content = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "input_text",
                        ["text"] = message.Content.Trim(),
                    },
                };

                foreach (var attachment in attachments)
                {
                    content.Add(CreateAttachmentInput(attachment));
                }

                input.Add(new JsonObject
                {
                    ["role"] = message.Role,
                    ["content"] = content,
                });
            }
            else
            {
                input.Add(new JsonObject
                {
                    ["role"] = message.Role,
                    ["content"] = message.Content.Trim(),
                });
            }
        }

        return input;
    }

    private static JsonObject CreateAttachmentInput(
        ConfiguratorChatAttachment attachment)
    {
        var base64 = Convert.ToBase64String(attachment.Data);
        var contentType = GetAttachmentContentType(
            attachment.FileName,
            attachment.ContentType
        );

        if (contentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase))
        {
            return new JsonObject
            {
                ["type"] = "input_image",
                ["detail"] = "high",
                ["image_url"] =
                    $"data:{contentType};base64,{base64}",
            };
        }

        return new JsonObject
        {
            ["type"] = "input_file",
            ["filename"] = attachment.FileName,
            ["file_data"] = $"data:{contentType};base64,{base64}",
        };
    }

    private void ValidateAttachments(
        IReadOnlyList<ConfiguratorChatAttachment> attachments)
    {
        var maxFiles = Math.Clamp(_options.MaxAttachmentFiles, 1, 8);
        var maxBytes = Math.Clamp(
            _options.MaxAttachmentBytes,
            256 * 1024,
            25 * 1024 * 1024
        );

        if (attachments.Count > maxFiles)
        {
            throw new ArgumentException(
                $"Attach no more than {maxFiles} files per message."
            );
        }

        foreach (var attachment in attachments)
        {
            if (attachment.Data.Length == 0 ||
                attachment.Data.Length > maxBytes)
            {
                throw new ArgumentException(
                    $"Attachment {attachment.FileName} has an invalid size."
                );
            }

            if (!IsSupportedAttachment(
                    attachment.FileName,
                    attachment.ContentType))
            {
                throw new ArgumentException(
                    $"Unsupported attachment type: {attachment.FileName}. " +
                    "Use PDF, PNG, JPEG or WebP."
                );
            }
        }
    }

    public async Task<string> TranscribeAsync(
        string fileName,
        string contentType,
        byte[] audio,
        CancellationToken cancellationToken)
    {
        var maxBytes = Math.Clamp(
            _options.MaxAudioBytes,
            256 * 1024,
            25 * 1024 * 1024
        );

        if (audio.Length == 0 || audio.Length > maxBytes)
        {
            throw new ArgumentException("Audio recording has an invalid size.");
        }

        var model = _options.TranscriptionModel?.Trim();

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                "OpenAI:Configurator:TranscriptionModel is empty."
            );
        }

        using var form = new MultipartFormDataContent();
        using var audioContent = new ByteArrayContent(audio);
        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType)
                ? "audio/webm"
                : contentType
        );

        form.Add(new StringContent(model), "model");
        form.Add(new StringContent("json"), "response_format");
        form.Add(
            new StringContent(
                "Electrical panel configurator. Expect room names, C16, C10, C20, C25, C32, RCD, UZO, 10 mA, 30 mA, volts, amperes and electrical appliance names."
            ),
            "prompt"
        );
        form.Add(audioContent, "file", string.IsNullOrWhiteSpace(fileName)
            ? "recording.webm"
            : fileName);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "audio/transcriptions"
        );
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", ResolveApiKey());
        request.Content = form;

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        var responseText = await response.Content.ReadAsStringAsync(
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                BuildOpenAiError(responseText, (int)response.StatusCode)
            );
        }

        var root = JsonNode.Parse(responseText) as JsonObject;
        var text = root?["text"]?.GetValue<string>()?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                "OpenAI transcription response is missing text."
            );
        }

        return text;
    }

    private static bool IsSupportedAttachment(
        string fileName,
        string contentType)
    {
        try
        {
            _ = GetAttachmentContentType(fileName, contentType);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string GetAttachmentContentType(
        string fileName,
        string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var expected = extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => throw new ArgumentException("Unsupported attachment type."),
        };

        var supplied = contentType?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(supplied) &&
            supplied != "application/octet-stream" &&
            supplied != expected)
        {
            throw new ArgumentException("Attachment content type does not match its extension.");
        }

        return expected;
    }

    private static string BuildDeveloperPrompt(JsonElement snapshot)
    {
        var snapshotJson = JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions
            {
                WriteIndented = false,
            }
        );

        return $$"""
You are the AI assistant embedded in an electrical panel configurator.

Your job is to understand the user's project description, answer questions about the current visible configuration, and use the provided configurator tools when the user asks to create or change the project.

Important behavior:
- Respond in the same language as the user's latest message.
- Act as an EXECUTOR, not an interview assistant. When the user's requested action can be represented with the available tools, perform it immediately instead of asking for confirmation or repeating questions.
- The current configurator state is authoritative. Use exact floorId and roomId values from it for targeted edits.
- Never ask the user to repeat or confirm a value that is already present in the current snapshot. Preserve all existing settings that the user did not explicitly ask to change.
- Do not ask for a project name when the user did not request a rename. Preserve the current project name.
- If floor or room names are omitted but the requested structure is clear, create sensible neutral names such as "Floor 1", "Room 1", "Room 2" instead of asking.
- Tool calls are queued and applied by the React frontend only after your turn finishes. They are NOT saved to the server automatically.
- Never claim that a project was saved or persisted. Briefly say what you changed; the user can review and press Save.
- Do not generate or modify projectId, floor UUIDs, or room UUIDs.
- Use replace_project for a complete new project. For unspecified visible project-level settings, preserve values from the current snapshot instead of inventing values.
- For targeted changes, modify only what the user requested. Do not opportunistically change unrelated settings.
- main3PN can only be enabled for a three-phase project with mainBreaker enabled.
- nonDisconnectableLine must be false for a three-phase project.
- Available main amperages: 25, 32, 63 A. Change main amperage only when the user explicitly requests it; otherwise preserve the snapshot value.
- Available shield widths: 12, 16, 24 modules per DIN rail. Change shield width only when explicitly requested; otherwise preserve it.
- Available voltage standards: 220 or 230 V. Change voltage only when explicitly requested; otherwise preserve it.
- Available breaker ratings: 2, 4, 6, 10, 16, 20, 25, 32 A.
- Available RCD sensitivities: 10 or 30 mA.
- PRODUCT DEFAULT FOR NEW CONSUMER LINES: if the user does not explicitly specify a breaker rating, use C16 (breakerAmperage=16). If the user does not explicitly specify RCD sensitivity, use 30 mA (rcdMilliAmps=30). Do not ask for these values when omitted. Explicit user values override these defaults.
- These C16/30 mA values are configurator defaults, not a claim that they are an engineering recommendation for every installation.
- Estimated catalog loads may use only these stable catalog IDs:
  lighting.general = Lighting, 400 W
  sockets.general = Sockets, 2500 W
  appliance.dishwasher = Dishwasher, 1800 W
  appliance.oven = Oven, 3000 W
  appliance.hob = Hob, 6600 W
  appliance.washing-machine = Washing Machine, 2000 W
  appliance.tumble-dryer = Tumble Dryer, 2500 W
  hvac.air-conditioner = Air Conditioner, 1500 W
- For a catalog load with powerSource=estimated, use its catalogTypeId and set powerWatts to null. The frontend resolves the stored estimate.
- For a custom/manual load, set catalogTypeId to null, powerSource to manual, and provide the explicit non-negative powerWatts supplied by the user.
- In replace_project, the project.assumptions field is metadata and MUST always be a JSON array of strings. If there are no material assumptions, send assumptions: []. Never send assumptions as a single string, null, or object.
- After tool calls, briefly summarize what you changed. Mention assumptions only when they materially affect the result; do not turn ordinary product defaults into follow-up questions.
- If the latest user message includes an attached floor plan, drawing, screenshot, or PDF, inspect it as part of the user request. Use visible labels and layout to infer floors/rooms when reasonably clear, then execute the requested configurator changes without asking the user to restate information that is visible in the attachment.
- Do not invent rooms or labels that are not reasonably supported by the attachment. If a detail is unreadable but not necessary for the requested action, proceed with sensible neutral names instead of blocking execution.
- Do not output raw tool JSON to the user.

CURRENT CONFIGURATOR SNAPSHOT JSON:
{{snapshotJson}}
""";
    }

    private static string ExtractOutputText(JsonArray output)
    {
        var parts = new List<string>();

        foreach (var itemNode in output)
        {
            if (itemNode is not JsonObject item ||
                item["type"]?.GetValue<string>() != "message" ||
                item["content"] is not JsonArray content)
            {
                continue;
            }

            foreach (var contentNode in content)
            {
                if (contentNode is JsonObject contentItem &&
                    contentItem["type"]?.GetValue<string>() == "output_text" &&
                    contentItem["text"] is JsonValue textValue &&
                    textValue.TryGetValue<string>(out var text) &&
                    !string.IsNullOrWhiteSpace(text))
                {
                    parts.Add(text.Trim());
                }
            }
        }

        return string.Join("\n\n", parts);
    }

    private static string RequireNodeString(
        JsonObject value,
        string key)
    {
        if (value[key] is JsonValue node &&
            node.TryGetValue<string>(out var text) &&
            !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        throw new InvalidOperationException(
            $"OpenAI function call is missing {key}."
        );
    }

    private static string BuildOpenAiError(
        string responseText,
        int statusCode)
    {
        try
        {
            var root = JsonNode.Parse(responseText) as JsonObject;
            var message = root?["error"]?["message"]?.GetValue<string>();

            if (!string.IsNullOrWhiteSpace(message))
            {
                return $"OpenAI API error ({statusCode}): {message}";
            }
        }
        catch
        {
            // Fall through to a safe generic error.
        }

        return $"OpenAI API request failed with HTTP {statusCode}.";
    }
}
