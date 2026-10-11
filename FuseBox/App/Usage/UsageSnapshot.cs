namespace FuseBox.App.Usage
{
    public sealed record UsageSnapshot(
        int Used,
        int Limit,
        int Remaining,
        DateTime ResetAtUtc);

    public sealed record UsageConsumeResult(
        bool Allowed,
        UsageSnapshot Snapshot);
}
