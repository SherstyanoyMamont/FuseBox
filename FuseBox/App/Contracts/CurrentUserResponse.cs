namespace FuseBox.App.Contracts.Auth
{
    public sealed class CurrentUserResponse
    {
        public int Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Plan { get; init; } = string.Empty;
        public CurrentUserFeaturesResponse Features { get; init; } = new();
        public CurrentUserLimitsResponse Limits { get; init; } = new();
    }

    public sealed class CurrentUserFeaturesResponse
    {
        public bool OnePhase { get; init; }
        public bool ThreePhase { get; init; }
        public bool SchemaEditor { get; init; }
        public bool ThreeD { get; init; }
        public bool VrMr { get; init; }
    }

    public sealed class CurrentUserLimitsResponse
    {
        public int AiRequestsPerDay { get; init; }
    }
}
