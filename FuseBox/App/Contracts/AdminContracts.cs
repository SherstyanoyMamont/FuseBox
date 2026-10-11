namespace FuseBox.App.Contracts.Admin
{
    public sealed class AdminUsersPageResponse
    {
        public IReadOnlyList<AdminUserListItemResponse> Items { get; init; } =
            Array.Empty<AdminUserListItemResponse>();
        public int Total { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }

    public sealed class AdminUserListItemResponse
    {
        public int Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Plan { get; init; } = string.Empty;
        public string SubscriptionStatus { get; init; } = string.Empty;
        public string SubscriptionProvider { get; init; } = string.Empty;
        public bool HasActiveEntitlementOverride { get; init; }
        public DateTime? EntitlementOverrideExpiresAtUtc { get; init; }
        public AdminAiUsageResponse AiUsage { get; init; } = new();
        public int Projects { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime UpdatedAtUtc { get; init; }
    }

    public sealed class AdminUserDetailResponse
    {
        public int Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;

        // Effective commercial plan after resolving billing + admin override.
        public string Plan { get; init; } = string.Empty;

        // Raw billing plan. This remains independent from temporary overrides.
        public string SubscriptionPlan { get; init; } = string.Empty;
        public string SubscriptionStatus { get; init; } = string.Empty;
        public string SubscriptionProvider { get; init; } = string.Empty;
        public DateTime? CurrentPeriodStartUtc { get; init; }
        public DateTime? CurrentPeriodEndUtc { get; init; }
        public bool CancelAtPeriodEnd { get; init; }

        public AdminEntitlementOverrideResponse? EntitlementOverride { get; init; }
        public AdminAiUsageResponse AiUsage { get; init; } = new();
        public AdminFeaturesResponse Features { get; init; } = new();
        public int Projects { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime UpdatedAtUtc { get; init; }
    }

    public sealed class AdminEntitlementOverrideResponse
    {
        public string Plan { get; init; } = string.Empty;
        public DateTime ExpiresAtUtc { get; init; }
        public bool Active { get; init; }
    }

    public sealed class AdminAiUsageResponse
    {
        public int Used { get; init; }
        public int Limit { get; init; }
        public int Remaining { get; init; }
        public DateTime ResetAtUtc { get; init; }
    }

    public sealed class AdminFeaturesResponse
    {
        public bool OnePhase { get; init; }
        public bool ThreePhase { get; init; }
        public bool SchemaEditor { get; init; }
        public bool ThreeD { get; init; }
        public bool VrMr { get; init; }
    }

    public sealed class AdminSummaryResponse
    {
        public int Users { get; init; }
        public int ProUsers { get; init; }
        public int FreeUsers { get; init; }
        public int SuspendedUsers { get; init; }
        public int AdminUsers { get; init; }
        public int AiRequestsToday { get; init; }
    }


    public sealed class AdminAuditPageResponse
    {
        public IReadOnlyList<AdminAuditItemResponse> Items { get; init; } =
            Array.Empty<AdminAuditItemResponse>();
        public int Total { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }

    public sealed class AdminAuditItemResponse
    {
        public long Id { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public int ActorUserId { get; init; }
        public string ActorEmail { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public int? TargetUserId { get; init; }
        public string? TargetEmail { get; init; }
        public string? Details { get; init; }
    }

    public sealed class AdminSetRoleRequest
    {
        public string Role { get; init; } = string.Empty;
    }

    public sealed class AdminSetEntitlementsRequest
    {
        public string Plan { get; init; } = string.Empty;
    }

    public sealed class AdminGrantEntitlementOverrideRequest
    {
        public string Plan { get; init; } = string.Empty;
        public DateTime ExpiresAtUtc { get; init; }
    }

    public sealed class AdminBillingReconciliationResponse
    {
        public DateTime StartedAtUtc { get; init; }
        public DateTime CompletedAtUtc { get; init; }
        public int Checked { get; init; }
        public int Updated { get; init; }
        public int Unchanged { get; init; }
        public int Failed { get; init; }
        public bool BackgroundEnabled { get; init; }
        public int IntervalMinutes { get; init; }
        public IReadOnlyList<AdminBillingReconciliationFailureResponse> Failures { get; init; } =
            Array.Empty<AdminBillingReconciliationFailureResponse>();
    }

    public sealed class AdminBillingReconciliationFailureResponse
    {
        public int UserId { get; init; }
        public string Code { get; init; } = string.Empty;
    }

    public sealed class AdminBillingHealthResponse
    {
        public string Environment { get; init; } = string.Empty;
        public bool BillingEnabled { get; init; }
        public bool ReadyForCurrentEnvironment { get; init; }
        public bool ReadyForProduction { get; init; }
        public bool ProviderCheckRequested { get; init; }
        public bool? ProviderApiReachable { get; init; }
        public DateTime CheckedAtUtc { get; init; }
        public IReadOnlyList<AdminBillingHealthCheckResponse> Checks { get; init; } =
            Array.Empty<AdminBillingHealthCheckResponse>();
        public int PaddleSubscriptions { get; init; }
        public int ActiveSubscriptions { get; init; }
        public int TrialingSubscriptions { get; init; }
        public int PastDueSubscriptions { get; init; }
        public int PausedSubscriptions { get; init; }
        public int CanceledSubscriptions { get; init; }
        public int UnprocessedWebhookEvents { get; init; }
        public DateTime? LastWebhookProcessedAtUtc { get; init; }
        public string? LastWebhookEventType { get; init; }
    }

    public sealed class AdminBillingHealthCheckResponse
    {
        public string Code { get; init; } = string.Empty;
        public string Severity { get; init; } = string.Empty;
        public bool Passed { get; init; }
        public string Message { get; init; } = string.Empty;
    }

}
