using FuseBox.App.Models;

namespace FuseBox.App.Subscriptions
{
    /// <summary>
    /// Temporary administrative commercial entitlement. This is deliberately
    /// separate from billing subscriptions: it never impersonates Paddle and
    /// expires automatically when ExpiresAtUtc is reached.
    /// </summary>
    public sealed class UserEntitlementOverride
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public PlanCode Plan { get; set; } = PlanCode.Pro;
        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
