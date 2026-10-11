
namespace FuseBox.App.Contracts.Common
{
    public static class ApiErrorCodes
    {
        public const string ValidationFailed = "validation.failed";

        public const string AuthenticationRequired = "auth.authentication_required";
        public const string InvalidCredentials = "auth.invalid_credentials";
        public const string AccountSuspended = "auth.account_suspended";
        public const string EmailAlreadyInUse = "auth.email_already_in_use";
        public const string InvalidResetToken = "auth.invalid_reset_token";
        public const string ResetTokenExpired = "auth.reset_token_expired";

        public const string QuotaAiExceeded = "quota.ai_exceeded";

        public const string FeatureThreePhaseRequired =
            "feature.three_phase_required";
        public const string FeatureAdvancedPanelRequired =
            "feature.advanced_panel_required";

        public const string ProjectNotFound = "project.not_found";
        public const string ProjectConflict = "project.conflict";
        public const string ProjectStorageFailed = "project.storage_failed";

        public const string SchemaRevisionConflict = "schema.revision_conflict";
        public const string SchemaCustomizationConflict = "schema.customization_conflict";
    }
}
