using System.Collections.Generic;

namespace FuseBox
{
    public class InitialSettings
    {
        public int PhasesCount { get; set; } // 1 or 3
        public int MainAmperage { get; set; } // 25А, 32А ...
        public int ShieldWidth { get; set; } // size 12, 16, 18
        public int VoltageStandard { get; set; } // 220 or 230
        public int PowerCoefficient { get; set; } // 0.5, 0.9, 1.0

        public InitialSettings()
        {
            PhasesCount = 3;
            MainAmperage = 25;
            ShieldWidth = 12;
            VoltageStandard = 220;
        }
    }
}
