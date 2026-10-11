using FuseBox.App.Contracts.Billing;
using FuseBox.App.DataBase;
using FuseBox.App.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Billing
{
    public sealed class BillingService
    {
        private readonly AppDbContext _context;
        private readonly PaddleClient _paddle;
        private readonly EntitlementService _entitlements;
        private readonly BillingProductionReadinessService _readiness;
        private readonly PaddleBillingOptions _options;

        public BillingService(
            AppDbContext context,
            PaddleClient paddle,
            EntitlementService entitlements,
            BillingProductionReadinessService readiness,
            IOptions<PaddleBillingOptions> options)
        {
            _context = context;
            _paddle = paddle;
            _entitlements = entitlements;
            _readiness = readiness;
            _options = options.Value;
        }

        public async Task<BillingCheckoutResponse> CreateProCheckoutAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            _readiness.EnsureSafeForCheckout();

            if (!_options.IsCheckoutConfigured)
            {
                throw new BillingNotConfiguredException();
            }

            var user = await _context.Users
                .Include(candidate => candidate.Subscription)
                .Include(candidate => candidate.EntitlementOverride)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == userId,
                    cancellationToken)
                ?? throw new BillingAccountNotFoundException();

            if (_entitlements.Resolve(
                user.Subscription,
                user.EntitlementOverride).Plan == PlanCode.Pro)
            {
                throw new BillingAlreadyProException();
            }

            var subscription = user.Subscription;

            if (subscription != null &&
                HasProviderStateFromAnotherEnvironment(subscription))
            {
                ResetProviderStateForCurrentEnvironment(subscription);
            }

            if (subscription == null)
            {
                var now = DateTime.UtcNow;
                subscription = new UserSubscription
                {
                    User = user,
                    Provider = SubscriptionProvider.Internal,
                    Plan = PlanCode.Free,
                    Status = SubscriptionStatus.Active,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                _context.UserSubscriptions.Add(subscription);
                user.Subscription = subscription;
            }

            var customerId = subscription.ProviderCustomerId;
            if (string.IsNullOrWhiteSpace(customerId))
            {
                customerId = await _paddle.CreateCustomerAsync(
                    user.Email,
                    user.Id,
                    cancellationToken);

                subscription.ProviderEnvironment =
                    _options.NormalizedEnvironment;
                subscription.ProviderCustomerId = customerId;
                subscription.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var transactionId = await _paddle.CreateProTransactionAsync(
                customerId,
                user.Id,
                cancellationToken);

            return new BillingCheckoutResponse
            {
                Provider = "paddle",
                Environment = _options.IsSandbox
                    ? "sandbox"
                    : "production",
                ClientSideToken = _options.ClientSideToken,
                TransactionId = transactionId
            };
        }

        private bool HasProviderStateFromAnotherEnvironment(
            UserSubscription subscription)
        {
            var hasProviderState =
                subscription.Provider == SubscriptionProvider.Paddle ||
                !string.IsNullOrWhiteSpace(subscription.ProviderCustomerId) ||
                !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId);

            return hasProviderState &&
                !string.Equals(
                    subscription.ProviderEnvironment,
                    _options.NormalizedEnvironment,
                    StringComparison.OrdinalIgnoreCase);
        }

        private void ResetProviderStateForCurrentEnvironment(
            UserSubscription subscription)
        {
            subscription.Provider = SubscriptionProvider.Internal;
            subscription.ProviderEnvironment = null;
            subscription.ProviderCustomerId = null;
            subscription.ProviderSubscriptionId = null;
            subscription.ProviderUpdatedAtUtc = null;
            subscription.Plan = PlanCode.Free;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStartUtc = null;
            subscription.CurrentPeriodEndUtc = null;
            subscription.CancelAtPeriodEnd = false;
            subscription.UpdatedAtUtc = DateTime.UtcNow;
        }

        public async Task<BillingPortalResponse> CreatePortalSessionAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            _readiness.EnsureSafeForCheckout();

            if (!_options.IsPortalConfigured)
            {
                throw new BillingPortalNotConfiguredException();
            }

            var subscription = await _context.UserSubscriptions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.UserId == userId,
                    cancellationToken);

            if (subscription == null)
            {
                var userExists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        candidate => candidate.Id == userId,
                        cancellationToken);

                if (!userExists)
                {
                    throw new BillingAccountNotFoundException();
                }

                throw new BillingPortalUnavailableException();
            }

            if (subscription.Provider != SubscriptionProvider.Paddle ||
                !string.Equals(
                    subscription.ProviderEnvironment,
                    _options.NormalizedEnvironment,
                    StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(
                    subscription.ProviderCustomerId))
            {
                throw new BillingPortalUnavailableException();
            }

            var portalUrl = await _paddle.CreateCustomerPortalSessionAsync(
                subscription.ProviderCustomerId,
                subscription.ProviderSubscriptionId,
                cancellationToken);

            return new BillingPortalResponse
            {
                Provider = "paddle",
                PortalUrl = portalUrl
            };
        }
    }

    public sealed class BillingNotConfiguredException : Exception { }
    public sealed class BillingAlreadyProException : Exception { }
    public sealed class BillingAccountNotFoundException : Exception { }
    public sealed class BillingPortalNotConfiguredException : Exception { }
    public sealed class BillingPortalUnavailableException : Exception { }
}
