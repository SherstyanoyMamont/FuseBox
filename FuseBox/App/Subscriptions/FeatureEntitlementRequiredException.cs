using FuseBox.App.Contracts.Common;

namespace FuseBox.App.Subscriptions
{
    public sealed class FeatureEntitlementRequiredException : Exception
    {
        public FeatureEntitlementRequiredException(
            string code,
            string requiredPlan,
            string message)
            : base(message)
        {
            Code = code;
            RequiredPlan = requiredPlan;
        }

        public string Code { get; }
        public string RequiredPlan { get; }

        public static FeatureEntitlementRequiredException ThreePhase() =>
            new(
                ApiErrorCodes.FeatureThreePhaseRequired,
                "pro",
                "3-phase calculation is available on Pro.");
    }
}
