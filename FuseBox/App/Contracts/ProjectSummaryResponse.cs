namespace FuseBox.App.Contracts.Projects
{
    public sealed class ProjectSummaryResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; init; }
        public DateTimeOffset UpdatedAtUtc { get; init; }
    }
}
