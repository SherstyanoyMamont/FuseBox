using System.Collections.Generic;

namespace FuseBox
{
    public class Shield
    {
        public bool MainCircuitBreaker { get; set; }
        public bool SurgeProtectionKit { get; set; }
        public bool LoadSwitch2P { get; set; }
        public bool ModularContactor { get; set; }
        public bool DinRailMeter { get; set; }
        public bool FireProtectionUZO { get; set; }
        public bool VoltageRelay { get; set; }
        public bool DinRailSocket { get; set; }
        public bool NonDisconnectableLine { get; set; }
        public bool LoadSwitch { get; set; }
        public bool CrossModule { get; set; }
        public int CountOfDINLines { get; set; }
        public List<List<IFuse>> Fuses { get; set; } = new() ; // List of devices // Список в списке!
        public void AddFuse(List<IFuse> fuse)                  // Interface IFuse*
        {
            Fuses.Add(fuse);
            ModularContactor = false;
        }
    }
}
