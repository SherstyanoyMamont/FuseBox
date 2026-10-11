namespace FuseBox.App.Administration
{
    public sealed class AdminAuditLog
    {
        public long Id { get; set; }

        public int ActorUserId { get; set; }
        public string ActorEmail { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public int? TargetUserId { get; set; }
        public string? TargetEmail { get; set; }

        // Human-readable, deliberately non-secret metadata such as
        // "status: active -> suspended" or an override expiry timestamp.
        public string? Details { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public static class AdminAuditActions
    {
        public const string SuspendUser = "SUSPEND_USER";
        public const string ActivateUser = "ACTIVATE_USER";
        public const string PromoteAdmin = "PROMOTE_ADMIN";
        public const string DemoteAdmin = "DEMOTE_ADMIN";
        public const string SetInternalPlan = "SET_INTERNAL_PLAN";
        public const string GrantEntitlement = "GRANT_ENTITLEMENT";
        public const string RemoveEntitlement = "REMOVE_ENTITLEMENT";
        public const string ResetAiUsage = "RESET_AI_USAGE";
        public const string ForceLogout = "FORCE_LOGOUT";
        public const string ReconcileBilling = "RECONCILE_BILLING";
        public const string CreateComponent = "CREATE_COMPONENT";
        public const string UpdateComponent = "UPDATE_COMPONENT";
        public const string ArchiveComponent = "ARCHIVE_COMPONENT";
        public const string RestoreComponent = "RESTORE_COMPONENT";
        public const string UploadComponentImage = "UPLOAD_COMPONENT_IMAGE";
        public const string DeleteComponentImage = "DELETE_COMPONENT_IMAGE";
        public const string UploadComponentModel = "UPLOAD_COMPONENT_MODEL";
        public const string DeleteComponentModel = "DELETE_COMPONENT_MODEL";
    }
}
