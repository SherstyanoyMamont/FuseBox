using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts
{
    public sealed class FuseBoxSettingsRequest
    {
        public bool MainBreaker { get; set; }
        public bool Main3PN { get; set; }
        public bool SurgeProtection { get; set; }
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

        [Range(1, int.MaxValue)]
        public int DINLines { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal Price { get; set; }
    }
}
