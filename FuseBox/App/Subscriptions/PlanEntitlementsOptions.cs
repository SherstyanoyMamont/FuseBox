namespace FuseBox.App.Subscriptions
{
    public sealed class PlanEntitlementsOptions
    {
        public PlanLimitsOptions Free { get; set; } = new()
        {
            AiRequestsPerDay = 5
        };

        public PlanLimitsOptions Pro { get; set; } = new()
        {
            AiRequestsPerDay = 100
        };
    }

    public sealed class PlanLimitsOptions
    {
        public int AiRequestsPerDay { get; set; }
    }
}
