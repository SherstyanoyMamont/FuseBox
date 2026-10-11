using FuseBox.App.DataBase;
using FuseBox.App.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Billing
{
    public sealed class BillingReconciliationService
    {
        private readonly AppDbContext _context;
        private readonly PaddleClient _paddle;
        private readonly PaddleBillingOptions _options;
        private readonly ILogger<BillingReconciliationService> _logger;

        public BillingReconciliationService(
            AppDbContext context,
            PaddleClient paddle,
            IOptions<PaddleBillingOptions> options,
            ILogger<BillingReconciliationService> logger)
        {
            _context = context;
            _paddle = paddle;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<BillingReconciliationResult> ReconcileAllAsync(
            CancellationToken cancellationToken = default)
        {
            if (!_options.IsReconciliationConfigured)
            {
                throw new BillingReconciliationNotConfiguredException();
            }

            var startedAtUtc = DateTime.UtcNow;

            // Known Paddle subscriptions are always checked. We also include
            // Free/Internal rows that already have a Paddle customer id but no
            // subscription id: this is the state left behind when checkout was
            // created successfully but subscription.created was missed.
            var providerEnvironment = _options.NormalizedEnvironment;

            var candidates = await _context.UserSubscriptions
                .AsNoTracking()
                .Where(subscription =>
                    subscription.ProviderEnvironment == providerEnvironment &&
                    ((subscription.Provider == SubscriptionProvider.Paddle &&
                      subscription.ProviderSubscriptionId != null &&
                      subscription.ProviderSubscriptionId != string.Empty) ||
                     (subscription.Provider == SubscriptionProvider.Internal &&
                      subscription.Plan == PlanCode.Free &&
                      subscription.ProviderCustomerId != null &&
                      subscription.ProviderCustomerId != string.Empty &&
                      subscription.ProviderSubscriptionId == null)))
                .OrderBy(subscription => subscription.Id)
                .Select(subscription => new ReconciliationCandidate(
                    subscription.Id,
                    subscription.UserId,
                    subscription.ProviderSubscriptionId,
                    subscription.ProviderCustomerId))
                .ToArrayAsync(cancellationToken);

            var updated = 0;
            var unchanged = 0;
            var failures = new List<BillingReconciliationFailure>();

            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var snapshot = await ResolveSnapshotAsync(
                        candidate,
                        cancellationToken);

                    if (snapshot == null)
                    {
                        // Most commonly an abandoned checkout that created a
                        // Paddle customer but never created a subscription.
                        unchanged++;
                        continue;
                    }

                    var outcome = await ApplySnapshotAsync(
                        candidate,
                        snapshot,
                        cancellationToken);

                    if (outcome == ReconciliationOutcome.Updated)
                    {
                        updated++;
                    }
                    else
                    {
                        unchanged++;
                    }
                }
                catch (BillingReconciliationValidationException exception)
                {
                    failures.Add(new BillingReconciliationFailure(
                        candidate.UserId,
                        candidate.ProviderSubscriptionId,
                        exception.Code));

                    _logger.LogWarning(
                        exception,
                        "Paddle reconciliation validation failed for user {UserId}, subscription {SubscriptionId}, code {Code}.",
                        candidate.UserId,
                        candidate.ProviderSubscriptionId,
                        exception.Code);

                    _context.ChangeTracker.Clear();
                }
                catch (Exception exception)
                    when (exception is not OperationCanceledException)
                {
                    failures.Add(new BillingReconciliationFailure(
                        candidate.UserId,
                        candidate.ProviderSubscriptionId,
                        "provider_lookup_failed"));

                    _logger.LogError(
                        exception,
                        "Paddle reconciliation failed for user {UserId}, subscription {SubscriptionId}.",
                        candidate.UserId,
                        candidate.ProviderSubscriptionId);

                    _context.ChangeTracker.Clear();
                }
            }

            return new BillingReconciliationResult(
                startedAtUtc,
                DateTime.UtcNow,
                candidates.Length,
                updated,
                unchanged,
                failures);
        }

        private async Task<PaddleSubscriptionSnapshot?> ResolveSnapshotAsync(
            ReconciliationCandidate candidate,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(candidate.ProviderSubscriptionId))
            {
                return await _paddle.GetSubscriptionAsync(
                    candidate.ProviderSubscriptionId,
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(candidate.ProviderCustomerId))
            {
                throw new BillingReconciliationValidationException(
                    "customer_id_missing");
            }

            var subscriptions = await _paddle.ListSubscriptionsForCustomerAsync(
                candidate.ProviderCustomerId,
                cancellationToken);

            var matching = subscriptions
                .Where(subscription =>
                    subscription.PriceIds.Contains(_options.ProPriceId) &&
                    subscription.FuseBoxUserId == candidate.UserId)
                .OrderBy(subscription =>
                    subscription.Status == "canceled" ? 1 : 0)
                .ThenByDescending(subscription => subscription.UpdatedAtUtc)
                .ToArray();

            if (matching.Length > 1)
            {
                _logger.LogWarning(
                    "Multiple Paddle Pro subscriptions were found for FuseBox user {UserId}. Reconciliation selected {SubscriptionId}.",
                    candidate.UserId,
                    matching[0].SubscriptionId);
            }

            return matching.FirstOrDefault();
        }

        private async Task<ReconciliationOutcome> ApplySnapshotAsync(
            ReconciliationCandidate candidate,
            PaddleSubscriptionSnapshot snapshot,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(candidate.ProviderSubscriptionId) &&
                !string.Equals(
                    snapshot.SubscriptionId,
                    candidate.ProviderSubscriptionId,
                    StringComparison.Ordinal))
            {
                throw new BillingReconciliationValidationException(
                    "subscription_id_mismatch");
            }

            if (!string.IsNullOrWhiteSpace(candidate.ProviderCustomerId) &&
                !string.Equals(
                    candidate.ProviderCustomerId,
                    snapshot.CustomerId,
                    StringComparison.Ordinal))
            {
                throw new BillingReconciliationValidationException(
                    "customer_id_mismatch");
            }

            if (snapshot.FuseBoxUserId.HasValue &&
                snapshot.FuseBoxUserId.Value != candidate.UserId)
            {
                throw new BillingReconciliationValidationException(
                    "user_mapping_mismatch");
            }

            var hasConfiguredProPrice =
                snapshot.PriceIds.Contains(_options.ProPriceId);

            if (!TryMapStatus(snapshot.Status, out var mappedStatus))
            {
                throw new BillingReconciliationValidationException(
                    "unknown_subscription_status");
            }

            var subscription = await _context.UserSubscriptions
                .SingleOrDefaultAsync(
                    item => item.Id == candidate.SubscriptionId,
                    cancellationToken);

            if (subscription == null)
            {
                throw new BillingReconciliationValidationException(
                    "local_subscription_missing");
            }

            if (!string.Equals(
                    subscription.ProviderEnvironment,
                    _options.NormalizedEnvironment,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BillingReconciliationValidationException(
                    "provider_environment_changed_during_reconciliation");
            }

            if (subscription.Provider == SubscriptionProvider.Paddle &&
                !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId) &&
                !string.Equals(
                    subscription.ProviderSubscriptionId,
                    snapshot.SubscriptionId,
                    StringComparison.Ordinal))
            {
                throw new BillingReconciliationValidationException(
                    "subscription_changed_during_reconciliation");
            }

            if (subscription.Provider == SubscriptionProvider.Internal &&
                (subscription.Plan != PlanCode.Free ||
                 !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId)))
            {
                throw new BillingReconciliationValidationException(
                    "local_subscription_no_longer_discoverable");
            }

            // A webhook may have updated the local row after this API snapshot
            // was fetched. Never let an older remote snapshot roll it back.
            if (subscription.ProviderUpdatedAtUtc.HasValue &&
                snapshot.UpdatedAtUtc < subscription.ProviderUpdatedAtUtc.Value)
            {
                return ReconciliationOutcome.Unchanged;
            }

            var expectedPlan = hasConfiguredProPrice
                ? PlanCode.Pro
                : PlanCode.Free;

            var changed =
                subscription.Provider != SubscriptionProvider.Paddle ||
                !string.Equals(
                    subscription.ProviderEnvironment,
                    _options.NormalizedEnvironment,
                    StringComparison.OrdinalIgnoreCase) ||
                subscription.ProviderCustomerId != snapshot.CustomerId ||
                subscription.ProviderSubscriptionId != snapshot.SubscriptionId ||
                subscription.Plan != expectedPlan ||
                subscription.Status != mappedStatus ||
                subscription.CurrentPeriodStartUtc != snapshot.CurrentPeriodStartUtc ||
                subscription.CurrentPeriodEndUtc != snapshot.CurrentPeriodEndUtc ||
                subscription.CancelAtPeriodEnd != snapshot.CancelAtPeriodEnd ||
                subscription.ProviderUpdatedAtUtc != snapshot.UpdatedAtUtc;

            if (!changed)
            {
                return ReconciliationOutcome.Unchanged;
            }

            subscription.Provider = SubscriptionProvider.Paddle;
            subscription.ProviderEnvironment = _options.NormalizedEnvironment;
            subscription.ProviderCustomerId = snapshot.CustomerId;
            subscription.ProviderSubscriptionId = snapshot.SubscriptionId;
            subscription.ProviderUpdatedAtUtc = snapshot.UpdatedAtUtc;
            subscription.Plan = expectedPlan;
            subscription.Status = mappedStatus;
            subscription.CurrentPeriodStartUtc = snapshot.CurrentPeriodStartUtc;
            subscription.CurrentPeriodEndUtc = snapshot.CurrentPeriodEndUtc;
            subscription.CancelAtPeriodEnd = snapshot.CancelAtPeriodEnd;
            subscription.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();

            return ReconciliationOutcome.Updated;
        }

        private static bool TryMapStatus(
            string value,
            out SubscriptionStatus status)
        {
            status = value switch
            {
                "active" => SubscriptionStatus.Active,
                "trialing" => SubscriptionStatus.Trialing,
                "past_due" => SubscriptionStatus.PastDue,
                "paused" => SubscriptionStatus.Paused,
                "canceled" => SubscriptionStatus.Canceled,
                _ => SubscriptionStatus.Inactive
            };

            return value is
                "active" or
                "trialing" or
                "past_due" or
                "paused" or
                "canceled";
        }

        private sealed record ReconciliationCandidate(
            int SubscriptionId,
            int UserId,
            string? ProviderSubscriptionId,
            string? ProviderCustomerId);

        private enum ReconciliationOutcome
        {
            Updated,
            Unchanged
        }
    }

    public sealed record BillingReconciliationResult(
        DateTime StartedAtUtc,
        DateTime CompletedAtUtc,
        int Checked,
        int Updated,
        int Unchanged,
        IReadOnlyList<BillingReconciliationFailure> Failures)
    {
        public int Failed => Failures.Count;
    }

    public sealed record BillingReconciliationFailure(
        int UserId,
        string? ProviderSubscriptionId,
        string Code);

    public sealed class BillingReconciliationNotConfiguredException : Exception
    {
    }

    internal sealed class BillingReconciliationValidationException : Exception
    {
        public BillingReconciliationValidationException(string code)
            : base(code)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
