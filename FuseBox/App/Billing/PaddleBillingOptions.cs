namespace FuseBox.App.Billing
{
    public sealed class PaddleBillingOptions
    {
        public const string SectionName = "Billing:Paddle";

        public bool Enabled { get; set; }
        public string Environment { get; set; } = "sandbox";
        public string ApiKey { get; set; } = string.Empty;
        public string ClientSideToken { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string ProPriceId { get; set; } = string.Empty;
        public int WebhookToleranceSeconds { get; set; } = 5;

        // Stage 14: switching to live payments must be an explicit operator
        // action, not an accidental side effect of changing Environment.
        public bool ProductionModeConfirmed { get; set; }

        // The webhook endpoint is anonymous by design, so cap the raw request
        // body before allocating an unbounded MemoryStream.
        public int MaxWebhookBodyBytes { get; set; } = 512 * 1024;

        // Stage 13: background reconciliation is a safety net for missed
        // webhooks. It can be disabled independently without disabling billing.
        public bool ReconciliationEnabled { get; set; } = true;
        public int ReconciliationIntervalMinutes { get; set; } = 360;
        public int ReconciliationStartupDelaySeconds { get; set; } = 60;

        public string NormalizedEnvironment =>
            (Environment ?? string.Empty).Trim().ToLowerInvariant();

        public bool IsSandbox => NormalizedEnvironment == "sandbox";
        public bool IsProduction => NormalizedEnvironment == "production";
        public bool HasKnownEnvironment => IsSandbox || IsProduction;

        public string ApiBaseUrl => NormalizedEnvironment switch
        {
            "sandbox" => "https://sandbox-api.paddle.com/",
            "production" => "https://api.paddle.com/",
            _ => throw new InvalidOperationException(
                "Billing:Paddle:Environment must be 'sandbox' or 'production'.")
        };

        public bool ApiKeyMatchesEnvironment =>
            !string.IsNullOrWhiteSpace(ApiKey) &&
            (IsSandbox
                ? ApiKey.Contains("_sdbx_apikey_", StringComparison.Ordinal)
                : IsProduction &&
                  ApiKey.Contains("_live_apikey_", StringComparison.Ordinal));

        public bool ClientSideTokenMatchesEnvironment =>
            !string.IsNullOrWhiteSpace(ClientSideToken) &&
            (IsSandbox
                ? ClientSideToken.StartsWith("test_", StringComparison.Ordinal)
                : IsProduction &&
                  ClientSideToken.StartsWith("live_", StringComparison.Ordinal));

        public bool HasValidWebhookSecretShape =>
            !string.IsNullOrWhiteSpace(WebhookSecret) &&
            WebhookSecret.StartsWith("pdl_ntfset_", StringComparison.Ordinal);

        public bool HasValidProPriceIdShape =>
            !string.IsNullOrWhiteSpace(ProPriceId) &&
            ProPriceId.StartsWith("pri_", StringComparison.Ordinal);

        public bool IsApiConfigured =>
            Enabled &&
            HasKnownEnvironment &&
            ApiKeyMatchesEnvironment;

        public bool IsCheckoutConfigured =>
            IsApiConfigured &&
            ClientSideTokenMatchesEnvironment &&
            HasValidProPriceIdShape &&
            (!IsProduction || ProductionModeConfirmed);

        public bool IsPortalConfigured => IsApiConfigured;

        public bool IsWebhookConfigured =>
            Enabled &&
            HasKnownEnvironment &&
            HasValidWebhookSecretShape &&
            HasValidProPriceIdShape;

        public bool IsReconciliationConfigured =>
            IsApiConfigured &&
            HasValidProPriceIdShape;

        public TimeSpan ReconciliationInterval => TimeSpan.FromMinutes(
            Math.Clamp(ReconciliationIntervalMinutes, 15, 24 * 60));

        public TimeSpan ReconciliationStartupDelay => TimeSpan.FromSeconds(
            Math.Clamp(ReconciliationStartupDelaySeconds, 0, 15 * 60));

        public int EffectiveMaxWebhookBodyBytes =>
            Math.Clamp(MaxWebhookBodyBytes, 16 * 1024, 2 * 1024 * 1024);
    }
}
