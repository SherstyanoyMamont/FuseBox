namespace FuseBox.App.Models
{
    public static class ConsumerPowerSourceValues
    {
        public const string Estimated = "estimated";
        public const string Manual = "manual";

        public static bool IsSupported(string? value)
        {
            return value == Estimated || value == Manual;
        }
    }
}
