using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using FuseBox.App.Models.BaseAbstract;
using System.Text.Json.Serialization;

namespace FuseBox
{
    public class Consumer : BaseElectrical, IZone
    {
        public int RoomId { get; set; }

        [JsonIgnore]
        public Room Room { get; set; } = null!;

        public Consumer()
        {
        }

        public Consumer(string name, int maxLoad)
        {
            Name = name;
            Amper = maxLoad;
        }

        // Nullable: legacy rows can have unknown power. Never derive watts
        // from the historical Amper value automatically.
        public double? PowerWatts { get; set; }

        // Stable catalog identifier used when the load originated from the
        // estimate catalog. Null means a custom or pre-catalog load.
        public string? CatalogTypeId { get; set; }

        // "estimated" or "manual". Nullable only for true legacy rows.
        public string? PowerSource { get; set; }

        public int BreakerAmperage { get; set; } = 16;

        public int RcdMilliAmps { get; set; } = 30;
    }
}
