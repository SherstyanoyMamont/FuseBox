namespace FuseBox.App.Contracts.Billing
{
    public sealed class BillingPortalResponse
    {
        public string Provider { get; init; } = "paddle";
        public string PortalUrl { get; init; } = string.Empty;
    }
}
