using System.Security.Claims;
using FuseBox.App.Billing;
using FuseBox.App.Contracts.Admin;
using FuseBox.App.Contracts.Common;
using FuseBox.App.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Administration
{
    [ApiController]
    [Authorize(Policy = "Admin")]
    [Route("api/admin/billing")]
    public sealed class AdminBillingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BillingReconciliationService _reconciliation;
        private readonly BillingProductionReadinessService _readiness;
        private readonly PaddleBillingOptions _options;

        public AdminBillingController(
            AppDbContext context,
            BillingReconciliationService reconciliation,
            BillingProductionReadinessService readiness,
            IOptions<PaddleBillingOptions> options)
        {
            _context = context;
            _reconciliation = reconciliation;
            _readiness = readiness;
            _options = options.Value;
        }

        [HttpGet("health")]
        public async Task<ActionResult<AdminBillingHealthResponse>> Health(
            [FromQuery] bool checkProvider = false,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await _readiness.GetSnapshotAsync(
                checkProvider,
                cancellationToken);

            return Ok(new AdminBillingHealthResponse
            {
                Environment = snapshot.Environment,
                BillingEnabled = snapshot.BillingEnabled,
                ReadyForCurrentEnvironment = snapshot.ReadyForCurrentEnvironment,
                ReadyForProduction = snapshot.ReadyForProduction,
                ProviderCheckRequested = snapshot.ProviderCheckRequested,
                ProviderApiReachable = snapshot.ProviderApiReachable,
                CheckedAtUtc = snapshot.CheckedAtUtc,
                Checks = snapshot.Checks
                    .Select(check => new AdminBillingHealthCheckResponse
                    {
                        Code = check.Code,
                        Severity = check.Severity,
                        Passed = check.Passed,
                        Message = check.Message
                    })
                    .ToArray(),
                PaddleSubscriptions = snapshot.PaddleSubscriptions,
                ActiveSubscriptions = snapshot.ActiveSubscriptions,
                TrialingSubscriptions = snapshot.TrialingSubscriptions,
                PastDueSubscriptions = snapshot.PastDueSubscriptions,
                PausedSubscriptions = snapshot.PausedSubscriptions,
                CanceledSubscriptions = snapshot.CanceledSubscriptions,
                UnprocessedWebhookEvents = snapshot.UnprocessedWebhookEvents,
                LastWebhookProcessedAtUtc = snapshot.LastWebhookProcessedAtUtc,
                LastWebhookEventType = snapshot.LastWebhookEventType
            });
        }

        [HttpPost("reconcile")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<AdminBillingReconciliationResponse>> Reconcile(
            CancellationToken cancellationToken)
        {
            if (!_options.IsReconciliationConfigured)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    Error(
                        "admin.billing_reconciliation_not_configured",
                        "Paddle reconciliation is not configured."));
            }

            var actorIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(actorIdValue, out var actorUserId))
            {
                return Unauthorized(Error(
                    "auth.authentication_required",
                    "Authentication is required."));
            }

            var actor = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == actorUserId)
                .Select(user => new { user.Id, user.Email })
                .SingleOrDefaultAsync(cancellationToken);

            if (actor == null)
            {
                return Unauthorized(Error(
                    "auth.authentication_required",
                    "Authentication is required."));
            }

            BillingReconciliationResult result;
            try
            {
                result = await _reconciliation.ReconcileAllAsync(
                    cancellationToken);
            }
            catch (BillingReconciliationNotConfiguredException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    Error(
                        "admin.billing_reconciliation_not_configured",
                        "Paddle reconciliation is not configured."));
            }

            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                ActorUserId = actor.Id,
                ActorEmail = actor.Email,
                Action = AdminAuditActions.ReconcileBilling,
                Details =
                    $"checked={result.Checked}; updated={result.Updated}; " +
                    $"unchanged={result.Unchanged}; failed={result.Failed}",
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new AdminBillingReconciliationResponse
            {
                StartedAtUtc = result.StartedAtUtc,
                CompletedAtUtc = result.CompletedAtUtc,
                Checked = result.Checked,
                Updated = result.Updated,
                Unchanged = result.Unchanged,
                Failed = result.Failed,
                BackgroundEnabled = _options.ReconciliationEnabled,
                IntervalMinutes = (int)_options.ReconciliationInterval.TotalMinutes,
                Failures = result.Failures
                    .Take(25)
                    .Select(failure => new AdminBillingReconciliationFailureResponse
                    {
                        UserId = failure.UserId,
                        Code = failure.Code
                    })
                    .ToArray()
            });
        }

        private static ApiErrorResponse Error(
            string code,
            string message)
        {
            return new ApiErrorResponse
            {
                Code = code,
                Message = message
            };
        }
    }
}
