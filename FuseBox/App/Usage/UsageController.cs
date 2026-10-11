using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuseBox.App.Usage
{
    [ApiController]
    [Authorize]
    [Route("api/account/usage")]
    public sealed class UsageController : ControllerBase
    {
        private readonly UsageService _usage;

        public UsageController(UsageService usage)
        {
            _usage = usage;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var ai = await _usage.GetAiUsageAsync(
                userId,
                cancellationToken);

            return Ok(new
            {
                aiRequests = new
                {
                    used = ai.Used,
                    limit = ai.Limit,
                    remaining = ai.Remaining,
                    resetAtUtc = ai.ResetAtUtc
                }
            });
        }
    }
}
