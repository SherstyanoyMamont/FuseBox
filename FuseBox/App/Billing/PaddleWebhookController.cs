using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Billing
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/billing/paddle")]
    public sealed class PaddleWebhookController : ControllerBase
    {
        private readonly PaddleWebhookVerifier _verifier;
        private readonly PaddleWebhookProcessor _processor;
        private readonly PaddleBillingOptions _options;
        private readonly ILogger<PaddleWebhookController> _logger;

        public PaddleWebhookController(
            PaddleWebhookVerifier verifier,
            PaddleWebhookProcessor processor,
            IOptions<PaddleBillingOptions> options,
            ILogger<PaddleWebhookController> logger)
        {
            _verifier = verifier;
            _processor = processor;
            _options = options.Value;
            _logger = logger;
        }

        [HttpPost("webhook")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> HandleWebhook(
            CancellationToken cancellationToken)
        {
            var maxBytes = _options.EffectiveMaxWebhookBodyBytes;

            if (Request.ContentLength.HasValue &&
                Request.ContentLength.Value > maxBytes)
            {
                return StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    new
                    {
                        code = "billing.webhook_payload_too_large",
                        message = "Paddle webhook payload is too large."
                    });
            }

            using var memory = new MemoryStream();
            var buffer = new byte[16 * 1024];

            while (true)
            {
                var read = await Request.Body.ReadAsync(
                    buffer.AsMemory(0, buffer.Length),
                    cancellationToken);

                if (read == 0)
                {
                    break;
                }

                if (memory.Length + read > maxBytes)
                {
                    return StatusCode(
                        StatusCodes.Status413PayloadTooLarge,
                        new
                        {
                            code = "billing.webhook_payload_too_large",
                            message = "Paddle webhook payload is too large."
                        });
                }

                await memory.WriteAsync(
                    buffer.AsMemory(0, read),
                    cancellationToken);
            }

            var rawBody = memory.ToArray();

            if (!_verifier.Verify(
                    Request.Headers["Paddle-Signature"].FirstOrDefault(),
                    rawBody,
                    DateTimeOffset.UtcNow))
            {
                return Unauthorized(new
                {
                    code = "billing.invalid_webhook_signature",
                    message = "Invalid Paddle webhook signature."
                });
            }

            try
            {
                var result = await _processor.ProcessAsync(
                    rawBody,
                    cancellationToken);

                return Ok(new
                {
                    received = true,
                    duplicate = result.Duplicate,
                    subscriptionUpdated = result.SubscriptionUpdated
                });
            }
            catch (Exception exception)
                when (exception is InvalidOperationException or
                    System.Text.Json.JsonException)
            {
                _logger.LogWarning(
                    exception,
                    "Rejected malformed Paddle webhook.");

                return BadRequest(new
                {
                    code = "billing.invalid_webhook",
                    message = "Invalid Paddle webhook payload."
                });
            }
        }
    }
}
