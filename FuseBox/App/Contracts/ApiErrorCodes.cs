namespace FuseBox.App.Contracts.Common
{
    public static class ApiErrorCodes
    {
        public const string ValidationFailed = "validation.failed";

        public const string AuthenticationRequired = "auth.authentication_required";
        public const string InvalidCredentials = "auth.invalid_credentials";
        public const string EmailAlreadyInUse = "auth.email_already_in_use";
        public const string InvalidResetToken = "auth.invalid_reset_token";
        public const string ResetTokenExpired = "auth.reset_token_expired";

        public const string ProjectNotFound = "project.not_found";
        public const string ProjectConflict = "project.conflict";
    }
}
