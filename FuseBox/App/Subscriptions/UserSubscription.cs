using FuseBox.App.Models;

namespace FuseBox.App.Subscriptions
{
    public sealed class UserSubscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public SubscriptionProvider Provider { get; set; } =
            SubscriptionProvider.Internal;

        // Provider identifiers are references only. FuseBox never stores card
        // data or raw payment credentials.
        // Paddle sandbox and live accounts are separate. Persist the
        // environment together with provider identifiers so sandbox state can
        // never become a live entitlement after deployment.
        public string? ProviderEnvironment { get; set; }
        public string? ProviderCustomerId { get; set; }
        public string? ProviderSubscriptionId { get; set; }
        public DateTime? ProviderUpdatedAtUtc { get; set; }

        public PlanCode Plan { get; set; } = PlanCode.Free;
        public SubscriptionStatus Status { get; set; } =
            SubscriptionStatus.Active;

        public DateTime? CurrentPeriodStartUtc { get; set; }
        public DateTime? CurrentPeriodEndUtc { get; set; }

        public bool CancelAtPeriodEnd { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
