namespace FuseBox.App.Subscriptions
{
    /// <summary>
    /// Effective commercial capabilities for one user. The frontend receives
    /// this computed result and never decides entitlements on its own.
    /// </summary>
    public sealed record EntitlementSnapshot(
        PlanCode Plan,
        bool CanUseOnePhase,
        bool CanUseThreePhase,
        bool CanUseSchemaEditor,
        bool CanUseThreeD,
        bool CanUseVrMr,
        int AiRequestsPerDay);
}
