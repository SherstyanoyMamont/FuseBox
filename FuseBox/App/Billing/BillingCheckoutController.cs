using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuseBox.App.Billing
{
    [ApiController]
    [Authorize]
    [Route("api/billing")]
    public sealed class BillingCheckoutController : ControllerBase
    {
        private readonly BillingService _billing;
        private readonly ILogger<BillingCheckoutController> _logger;

        public BillingCheckoutController(
            BillingService billing,
            ILogger<BillingCheckoutController> logger)
        {
            _billing = billing;
            _logger = logger;
        }

        [HttpPost("checkout/pro")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProCheckout(
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
                return Ok(await _billing.CreateProCheckoutAsync(
                    userId,
                    cancellationToken));
            }
            catch (BillingNotConfiguredException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        code = "billing.not_configured",
                        message = "Paddle checkout is not configured."
                    });
            }
            catch (BillingAlreadyProException)
            {
                return Conflict(new
                {
                    code = "billing.already_pro",
                    message = "This account already has Pro entitlements."
                });
            }
            catch (BillingAccountNotFoundException)
            {
                return Unauthorized();
            }
            catch (BillingUnsafeProductionConfigurationException exception)
            {
                _logger.LogError(
                    exception,
                    "Blocked Paddle production checkout because billing readiness checks failed for user {UserId}.",
                    userId);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        code = "billing.production_not_ready",
                        message = "Live billing is not ready. An administrator must complete the production readiness checks."
                    });
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(
                    exception,
                    "Paddle checkout creation failed for user {UserId}.",
                    userId);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        code = "billing.provider_error",
                        message = "Could not start Paddle checkout."
                    });
            }
        }
    }
}
