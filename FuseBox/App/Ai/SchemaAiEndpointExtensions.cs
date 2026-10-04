using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace FuseBox.Ai;

public static class SchemaAiEndpointExtensions
{
    public static IEndpointRouteBuilder MapFuseBoxSchemaAi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/ai/schema/chat",
            async (SchemaChatRequest request, SchemaAiService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await service.ChatAsync(request, cancellationToken));
                }
                catch (ArgumentException error)
                {
                    return Results.BadRequest(new { code = "ai.invalid_request", message = error.Message });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(499);
                }
                catch (InvalidOperationException error)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "AI schema assistant request failed",
                        detail: error.Message,
                        extensions: new Dictionary<string, object?> { ["code"] = "ai.provider_error" });
                }
            })
            .RequireAuthorization()
            .RequireRateLimiting("ai-daily")
            .WithName("SchemaAiChat")
            .WithTags("AI");

        return endpoints;
    }
}
