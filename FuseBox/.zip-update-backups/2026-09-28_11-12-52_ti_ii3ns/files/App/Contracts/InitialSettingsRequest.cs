using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts
{
    public sealed class InitialSettingsRequest
    {
        [AllowedValues(1, 3)]
        public int PhasesCount { get; set; }

        [AllowedValues(25, 32, 40, 50, 63)]
        public int MainAmperage { get; set; }

        [AllowedValues(12, 16, 18)]
        public int ShieldWidth { get; set; }

        [AllowedValues(220, 230)]
        public int VoltageStandard { get; set; }

        [Range(0, 2)]
        public double PowerCoefficient { get; set; }
    }
}
