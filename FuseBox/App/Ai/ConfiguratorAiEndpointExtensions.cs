using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FuseBox.App.Usage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace FuseBox.Ai;

public static class ConfiguratorAiEndpointExtensions
{
    public static IEndpointRouteBuilder MapFuseBoxConfiguratorAi(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/ai/configurator/chat",
            async (
                HttpContext httpContext,
                ConfiguratorChatRequest request,
                ConfiguratorAiService service,
                UsageService usage,
                CancellationToken cancellationToken) =>
            {
                var quotaFailure = await AiQuotaGuard.TryConsumeAsync(
                    httpContext,
                    usage,
                    cancellationToken);

                if (quotaFailure != null)
                {
                    return quotaFailure;
                }

                return await RunChatAsync(
                    () => service.ChatAsync(request, cancellationToken)
                );
            }
        )
        .RequireAuthorization()
        .RequireRateLimiting("ai-daily")
        .WithName("ConfiguratorAiChat")
        .WithTags("AI");

        endpoints.MapPost(
            "/api/ai/configurator/chat-with-files",
            async (
                HttpContext httpContext,
                HttpRequest httpRequest,
                ConfiguratorAiService service,
                UsageService usage,
                CancellationToken cancellationToken) =>
            {
                return await RunChatAsync(async () =>
                {
                    if (!httpRequest.HasFormContentType)
                    {
                        throw new ArgumentException(
                            "Multipart form data is required."
                        );
                    }

                    var form = await httpRequest.ReadFormAsync(
                        cancellationToken
                    );

                    var messagesJson = form["messages"].FirstOrDefault();
                    var snapshotJson = form["snapshot"].FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(messagesJson) ||
                        string.IsNullOrWhiteSpace(snapshotJson))
                    {
                        throw new ArgumentException(
                            "messages and snapshot are required."
                        );
                    }

                    var messages = JsonSerializer.Deserialize<
                        List<ConfiguratorChatMessage>>(messagesJson)
                        ?? throw new ArgumentException(
                            "Chat messages are invalid."
                        );

                    using var snapshotDocument = JsonDocument.Parse(
                        snapshotJson
                    );

                    var request = new ConfiguratorChatRequest(
                        messages,
                        snapshotDocument.RootElement.Clone()
                    );

                    var attachments = new List<ConfiguratorChatAttachment>();

                    foreach (var file in form.Files.GetFiles("files"))
                    {
                        await using var stream = file.OpenReadStream();
                        using var memory = new MemoryStream();
                        await stream.CopyToAsync(memory, cancellationToken);

                        attachments.Add(new ConfiguratorChatAttachment(
                            Path.GetFileName(file.FileName),
                            file.ContentType ?? string.Empty,
                            memory.ToArray()
                        ));
                    }

                    var quotaFailure = await AiQuotaGuard.TryConsumeAsync(
                        httpContext,
                        usage,
                        cancellationToken);

                    if (quotaFailure != null)
                    {
                        throw new AiQuotaRejectedException(quotaFailure);
                    }

                    return await service.ChatAsync(
                        request,
                        attachments,
                        cancellationToken
                    );
                });
            }
        )
        .RequireAuthorization()
        .RequireRateLimiting("ai-daily")
        .WithName("ConfiguratorAiChatWithFiles")
        .WithTags("AI");

        endpoints.MapPost(
            "/api/ai/configurator/transcribe",
            async (
                HttpContext httpContext,
                HttpRequest httpRequest,
                ConfiguratorAiService service,
                UsageService usage,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!httpRequest.HasFormContentType)
                    {
                        throw new ArgumentException(
                            "Multipart form data is required."
                        );
                    }

                    var form = await httpRequest.ReadFormAsync(
                        cancellationToken
                    );
                    var file = form.Files.GetFile("audio")
                        ?? throw new ArgumentException(
                            "Audio recording is required."
                        );

                    await using var stream = file.OpenReadStream();
                    using var memory = new MemoryStream();
                    await stream.CopyToAsync(memory, cancellationToken);

                    var quotaFailure = await AiQuotaGuard.TryConsumeAsync(
                        httpContext,
                        usage,
                        cancellationToken);

                    if (quotaFailure != null)
                    {
                        return quotaFailure;
                    }

                    var text = await service.TranscribeAsync(
                        Path.GetFileName(file.FileName),
                        file.ContentType ?? "audio/webm",
                        memory.ToArray(),
                        cancellationToken
                    );

                    return Results.Ok(
                        new ConfiguratorTranscriptionResponse(text)
                    );
                }
                catch (ArgumentException error)
                {
                    return Results.BadRequest(new
                    {
                        code = "ai.invalid_request",
                        message = error.Message,
                    });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(499);
                }
                catch (InvalidOperationException error)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "AI transcription failed",
                        detail: error.Message,
                        extensions: new Dictionary<string, object?>
                        {
                            ["code"] = "ai.provider_error",
                        }
                    );
                }
            }
        )
        .RequireAuthorization()
        .RequireRateLimiting("ai-transcription-daily")
        .WithName("ConfiguratorAiTranscribe")
        .WithTags("AI");

        return endpoints;
    }

    private static async Task<IResult> RunChatAsync(
        Func<Task<ConfiguratorChatResponse>> action)
    {
        try
        {
            var result = await action();
            return Results.Ok(result);
        }
        catch (AiQuotaRejectedException rejected)
        {
            return rejected.Result;
        }
        catch (ArgumentException error)
        {
            return Results.BadRequest(new
            {
                code = "ai.invalid_request",
                message = error.Message,
            });
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (InvalidOperationException error)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "AI assistant request failed",
                detail: error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "ai.provider_error",
                }
            );
        }
    }

    private sealed class AiQuotaRejectedException : Exception
    {
        public AiQuotaRejectedException(IResult result)
        {
            Result = result;
        }

        public IResult Result { get; }
    }
}
