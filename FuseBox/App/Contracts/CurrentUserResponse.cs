namespace FuseBox.App.Contracts.Auth
{
    public sealed class CurrentUserResponse
    {
        public int Id { get; init; }
        public string Email { get; init; } = string.Empty;
    }
}
