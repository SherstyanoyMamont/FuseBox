using System.Collections.Generic;

namespace FuseBox
{
    public class Shield
    {
        public bool MainCircuitBreaker { get; set; }
        public bool SurgeProtectionKit { get; set; }
        public bool LoadSwitch2P { get; set; }
        public bool ModularContactor { get; set; }
        public bool RailMeter { get; set; }
        public bool FireUZO { get; set; }
        public bool VoltageRelay { get; set; }
        public bool ThreePRelay { get; set; }
        public bool RailSocket { get; set; }
        public bool NDisconnectableLine { get; set; }
        public bool LoadSwitch { get; set; }
        public bool CrossModule { get; set; }
        public int DINLines { get; set; }
        public List<List<Module>> Fuses { get; set; } = new(); // List of devices // Список в списке!
    }
}
