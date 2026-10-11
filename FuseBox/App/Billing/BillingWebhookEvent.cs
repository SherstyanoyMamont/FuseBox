namespace FuseBox.App.Billing
{
    public sealed class BillingWebhookEvent
    {
        public long Id { get; set; }
        public string Provider { get; set; } = "Paddle";
        public string ProviderEnvironment { get; set; } = string.Empty;
        public string ProviderEventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public string PayloadSha256 { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
