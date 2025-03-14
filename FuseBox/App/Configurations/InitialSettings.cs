using FuseBox.App.Models.BaseAbstract;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FuseBox
{
    namespace FuseBox
    {
        public class InitialSettings : BaseEntity
        {
            [AllowedValues(1, 3, ErrorMessage = "PhasesCount only 1 or 3")]
            public int PhasesCount { get; set; } // 1 or 3

            [AllowedValues(25, 32, 40, 50, 63, ErrorMessage = "Main Amperage only: 25, 32, 40, 50 or 63")]
            public int MainAmperage { get; set; } // 25А, 32А ...

            [AllowedValues(12, 16, 18, ErrorMessage = "Shield Width only 12 or 16 or 18")]
            public int ShieldWidth { get; set; } // size 12, 16, 18

            [AllowedValues(220, 230, ErrorMessage = "Voltage Standard only 220 or 230")]
            public int VoltageStandard { get; set; } // 220 or 230

            [Range(0, 2, ErrorMessage = "Power Coefficient from 0 to 2")]
            public int PowerCoefficient { get; set; } // 0.5, 0.9, 1.0

            
            public InitialSettings() { }

            public InitialSettings(int phasesCount, int shieldWidth)
            {
                PhasesCount = phasesCount;
                ShieldWidth = shieldWidth;
            }


        }
    }
}
