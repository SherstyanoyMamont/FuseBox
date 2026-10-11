namespace FuseBox.App.Contracts
{
    public sealed class CsrfTokenResponse
    {
        public string Token { get; init; } = string.Empty;
    }
}
