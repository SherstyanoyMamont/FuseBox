using FuseBox.App.Billing;
using FuseBox.App.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Subscriptions
{
    /// <summary>
    /// Single source of truth for commercial permissions. Role is deliberately
    /// not considered here: Admin does not imply Pro and Pro does not imply Admin.
    /// Billing state and temporary admin overrides are resolved independently.
    /// </summary>
    public sealed class EntitlementService
    {
        public const string ThreePhaseFeature = "three_phase";
        public const string AiRequestsPerDayLimit = "ai_requests_per_day";

        private readonly AppDbContext _context;
        private readonly PlanEntitlementsOptions _options;
        private readonly PaddleBillingOptions _billingOptions;

        public EntitlementService(
            AppDbContext context,
            IOptions<PlanEntitlementsOptions> options,
            IOptions<PaddleBillingOptions> billingOptions)
        {
            _context = context;
            _options = options.Value;
            _billingOptions = billingOptions.Value;
        }

        public EntitlementSnapshot Resolve(
            UserSubscription? subscription,
            UserEntitlementOverride? entitlementOverride = null,
            DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;

            var paidStatus = subscription?.Status is
                SubscriptionStatus.Active or
                SubscriptionStatus.Trialing or
                SubscriptionStatus.PastDue;

            var providerEnvironmentMatches =
                subscription?.Provider != SubscriptionProvider.Paddle ||
                string.Equals(
                    subscription.ProviderEnvironment,
                    _billingOptions.NormalizedEnvironment,
                    StringComparison.OrdinalIgnoreCase);

            var subscriptionPro =
                subscription is { Plan: PlanCode.Pro } &&
                paidStatus &&
                providerEnvironmentMatches;

            var overridePro =
                entitlementOverride is { Plan: PlanCode.Pro } &&
                entitlementOverride.ExpiresAtUtc > now;

            var effectivePlan = subscriptionPro || overridePro
                ? PlanCode.Pro
                : PlanCode.Free;

            var configuredLimit = effectivePlan == PlanCode.Pro
                ? _options.Pro.AiRequestsPerDay
                : _options.Free.AiRequestsPerDay;

            return new EntitlementSnapshot(
                effectivePlan,
                CanUseOnePhase: true,
                CanUseThreePhase: effectivePlan == PlanCode.Pro,
                CanUseSchemaEditor: true,
                CanUseThreeD: true,
                CanUseVrMr: true,
                AiRequestsPerDay: Math.Clamp(configuredLimit, 0, 100000));
        }

        public async Task<EntitlementSnapshot> GetForUserAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _context.UserSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.UserId == userId,
                    cancellationToken);

            var entitlementOverride = await _context.UserEntitlementOverrides
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.UserId == userId,
                    cancellationToken);

            return Resolve(subscription, entitlementOverride);
        }

        public async Task<bool> HasAsync(
            int userId,
            string feature,
            CancellationToken cancellationToken = default)
        {
            var entitlements = await GetForUserAsync(userId, cancellationToken);

            return feature switch
            {
                ThreePhaseFeature => entitlements.CanUseThreePhase,
                _ => false
            };
        }

        public async Task<int?> GetLimitAsync(
            int userId,
            string limit,
            CancellationToken cancellationToken = default)
        {
            var entitlements = await GetForUserAsync(userId, cancellationToken);

            return limit switch
            {
                AiRequestsPerDayLimit => entitlements.AiRequestsPerDay,
                _ => null
            };
        }
    }
}
