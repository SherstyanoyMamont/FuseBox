namespace FuseBox.App.Contracts.Accounts
{
    public sealed class AccountOverviewResponse
    {
        public AccountProfileResponse Profile { get; init; } = new();
        public AccountSubscriptionResponse Subscription { get; init; } = new();
        public AccountUsageResponse Usage { get; init; } = new();
        public AccountFeaturesResponse Features { get; init; } = new();
        public AccountProjectsResponse Projects { get; init; } = new();
        public AccountBillingResponse Billing { get; init; } = new();
    }

    public sealed class AccountProfileResponse
    {
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
    }

    public sealed class AccountSubscriptionResponse
    {
        public string Plan { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public DateTime? CurrentPeriodStartUtc { get; init; }
        public DateTime? CurrentPeriodEndUtc { get; init; }
        public bool CancelAtPeriodEnd { get; init; }
    }

    public sealed class AccountUsageResponse
    {
        public AccountAiUsageResponse AiRequests { get; init; } = new();
    }

    public sealed class AccountAiUsageResponse
    {
        public int Used { get; init; }
        public int Limit { get; init; }
        public int Remaining { get; init; }
        public DateTime ResetAtUtc { get; init; }
    }

    public sealed class AccountFeaturesResponse
    {
        public bool OnePhase { get; init; }
        public bool ThreePhase { get; init; }
        public bool SchemaEditor { get; init; }
        public bool ThreeD { get; init; }
        public bool VrMr { get; init; }
    }

    public sealed class AccountProjectsResponse
    {
        public int Count { get; init; }
    }

    public sealed class AccountBillingResponse
    {
        public bool CheckoutAvailable { get; init; }
        public bool PortalAvailable { get; init; }
        public string Provider { get; init; } = "paddle";
        public string Environment { get; init; } = "sandbox";
    }
}
