namespace FuseBox
{
    public class InitialSettings
    {
        public int Phases { get; set; } // 1 or 3
        public int MainBreakerA { get; set; } // 25А, 32А ...
        public int ShieldWidth { get; set; } // size 12, 16, 18
        public int VoltageStandard { get; set; } // 220 or 230
    }
    public class ShieldDevice
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
        public List<SimpleFuse> Fuses { get; set; } = new(); // List of devices
        public void AddFuse(SimpleFuse fuse)
        {
            Fuses.Add(fuse);
            ModularContactor = false;
        }
    }
}
