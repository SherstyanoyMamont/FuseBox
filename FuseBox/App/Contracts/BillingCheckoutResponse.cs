namespace FuseBox.App.Contracts.Billing
{
    public sealed class BillingCheckoutResponse
    {
        public string Provider { get; init; } = "paddle";
        public string Environment { get; init; } = "sandbox";
        public string ClientSideToken { get; init; } = string.Empty;
        public string TransactionId { get; init; } = string.Empty;
    }
}
