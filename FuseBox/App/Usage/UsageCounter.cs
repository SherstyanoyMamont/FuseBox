namespace FuseBox.App.Usage
{
    public sealed class UsageCounter
    {
        public int UserId { get; set; }
        public string Metric { get; set; } = string.Empty;
        public DateTime PeriodStartUtc { get; set; }
        public int Count { get; set; }
    }
}
