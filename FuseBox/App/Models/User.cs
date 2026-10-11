
using FuseBox.App.Accounts;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Subscriptions;

namespace FuseBox.App.Models
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;

        // Null is reserved for pre-authentication legacy identities.
        // Every account created through /api/auth/register has a password hash.
        public string? PasswordHash { get; set; }

        public UserRole Role { get; set; } = UserRole.User;
        public UserStatus Status { get; set; } = UserStatus.Active;

        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public UserSubscription? Subscription { get; set; }
        public UserEntitlementOverride? EntitlementOverride { get; set; }

        public List<Project> Projects { get; set; } = new();
        public List<PasswordResetToken> PasswordResetTokens { get; set; } = new();
    }
}
