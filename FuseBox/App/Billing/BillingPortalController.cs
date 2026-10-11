using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuseBox.App.Billing
{
    [ApiController]
    [Authorize]
    [Route("api/billing")]
    public sealed class BillingPortalController : ControllerBase
    {
        private readonly BillingService _billing;
        private readonly ILogger<BillingPortalController> _logger;

        public BillingPortalController(
            BillingService billing,
            ILogger<BillingPortalController> logger)
        {
            _billing = billing;
            _logger = logger;
        }

        [HttpPost("portal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePortalSession(
            CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                return Ok(await _billing.CreatePortalSessionAsync(
                    userId,
                    cancellationToken));
            }
            catch (BillingPortalNotConfiguredException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        code = "billing.portal_not_configured",
                        message = "Paddle customer portal is not configured."
                    });
            }
            catch (BillingPortalUnavailableException)
            {
                return Conflict(new
                {
                    code = "billing.portal_unavailable",
                    message = "This account does not have a Paddle billing profile."
                });
            }
            catch (BillingAccountNotFoundException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(
                    exception,
                    "Paddle customer portal creation failed for user {UserId}.",
                    userId);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        code = "billing.provider_error",
                        message = "Could not open Paddle customer portal."
                    });
            }
        }
    }
}
