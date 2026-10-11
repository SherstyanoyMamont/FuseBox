using FuseBox.App.DataBase;
using FuseBox.App.Models;
using FuseBox.App.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Subscriptions
{
    /// <summary>
    /// Mutates subscription state. Until Stage 6, --set-plan remains an
    /// internal/manual test path and never impersonates a Paddle subscription.
    /// </summary>
    public sealed class SubscriptionService
    {
        private readonly AppDbContext _context;

        public SubscriptionService(AppDbContext context)
        {
            _context = context;
        }

        public UserSubscription CreateFree(User user, DateTime now)
        {
            return new UserSubscription
            {
                User = user,
                Provider = SubscriptionProvider.Internal,
                Plan = PlanCode.Free,
                Status = SubscriptionStatus.Active,
                CancelAtPeriodEnd = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }

        public async Task<UserSubscription> EnsureForUserAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            if (user.Subscription != null)
            {
                return user.Subscription;
            }

            var existing = await _context.UserSubscriptions
                .FirstOrDefaultAsync(
                    candidate => candidate.UserId == user.Id,
                    cancellationToken);

            if (existing != null)
            {
                user.Subscription = existing;
                return existing;
            }

            var now = DateTime.UtcNow;
            var subscription = CreateFree(user, now);

            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync(cancellationToken);

            user.Subscription = subscription;
            return subscription;
        }

        public async Task<UserSubscription> SetInternalPlanAsync(
            User user,
            PlanCode plan,
            CancellationToken cancellationToken = default)
        {
            var subscription = user.Subscription;

            if (subscription == null)
            {
                subscription = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(
                        candidate => candidate.UserId == user.Id,
                        cancellationToken);
            }

            if (subscription?.Provider == SubscriptionProvider.Paddle)
            {
                throw new InvalidOperationException(
                    "Paddle-managed subscriptions cannot be overwritten by an internal plan mutation.");
            }

            var now = DateTime.UtcNow;

            if (subscription == null)
            {
                subscription = CreateFree(user, now);
                _context.UserSubscriptions.Add(subscription);
                user.Subscription = subscription;
            }

            subscription.Provider = SubscriptionProvider.Internal;
            subscription.ProviderCustomerId = null;
            subscription.ProviderSubscriptionId = null;
            subscription.ProviderUpdatedAtUtc = null;
            subscription.Plan = plan;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStartUtc = null;
            subscription.CurrentPeriodEndUtc = null;
            subscription.CancelAtPeriodEnd = false;
            subscription.UpdatedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);
            user.Subscription = subscription;
            return subscription;
        }

        public async Task<User?> SetPlanByEmailAsync(
            string email,
            PlanCode plan,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = EmailNormalizer.Normalize(email);

            var user = await _context.Users
                .Include(candidate => candidate.Subscription)
                .FirstOrDefaultAsync(
                    candidate => candidate.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var subscription = user.Subscription ?? CreateFree(user, now);

            if (user.Subscription == null)
            {
                _context.UserSubscriptions.Add(subscription);
                user.Subscription = subscription;
            }

            // This command is intentionally marked Internal. Stage 6 Paddle
            // webhooks will be the only code allowed to persist Provider=Paddle.
            subscription.Provider = SubscriptionProvider.Internal;
            subscription.ProviderCustomerId = null;
            subscription.ProviderSubscriptionId = null;
            subscription.ProviderUpdatedAtUtc = null;
            subscription.Plan = plan;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStartUtc = null;
            subscription.CurrentPeriodEndUtc = null;
            subscription.CancelAtPeriodEnd = false;
            subscription.UpdatedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }
    }
}
