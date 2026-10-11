namespace FuseBox.App.Contracts.Auth
{
    public sealed class CsrfTokenResponse
    {
        public string Token { get; init; } = string.Empty;
    }
}
