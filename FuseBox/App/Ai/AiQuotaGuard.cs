using System.Security.Claims;
using FuseBox.App.Contracts.Common;
using FuseBox.App.Usage;
using Microsoft.AspNetCore.Http;

namespace FuseBox.Ai;

internal static class AiQuotaGuard
{
    public static async Task<IResult?> TryConsumeAsync(
        HttpContext httpContext,
        UsageService usage,
        CancellationToken cancellationToken)
    {
        var userIdValue = httpContext.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await usage.TryConsumeAiRequestAsync(
            userId,
            cancellationToken);

        if (result.Allowed)
        {
            return null;
        }

        var quota = result.Snapshot;

        return Results.Json(
            new
            {
                code = ApiErrorCodes.QuotaAiExceeded,
                message = "Daily AI request quota reached for the current plan.",
                limit = quota.Limit,
                used = quota.Used,
                resetAtUtc = quota.ResetAtUtc
            },
            statusCode: StatusCodes.Status429TooManyRequests);
    }
}
