using System.Collections.Generic;

namespace FuseBox
{
    public class InitialSettings
    {
        public int Phases { get; set; } // 1 or 3
        public int MainBreakerA { get; set; } // 25А, 32А ...
        public int ShieldWidth { get; set; } // size 12, 16, 18
        public int VoltageStandard { get; set; } // 220 or 230

        public InitialSettings()
        {
            Phases = 1;
            MainBreakerA = 25;
            ShieldWidth = 12;
            VoltageStandard = 220;
        }
    }
}
