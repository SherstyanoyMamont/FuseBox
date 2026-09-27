using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using System.Text.Json.Serialization;

namespace FuseBox
{
    public class Consumer : BaseElectrical, IZone
    {
        // Связь с комнатой
        public int RoomId { get; set; }
        [JsonIgnore]
        public Room Room { get; set; }

        //// Связь с FuseBoxUnit
        //public int FuseBoxUnitId { get; set; }
        //public FuseBoxUnit? FuseBoxUnit { get; set; }

        public Consumer() { }
        public Consumer(string name, int maxLoad)        // Для тестов
        {
            this.Name = name;
            this.Amper = maxLoad;
        }

        // Nullable: у старых записей мощность неизвестна.
        // Старое Amper нельзя автоматически считать мощностью.
        public double? PowerWatts { get; set; }

        public int BreakerAmperage { get; set; } = 16;

        public int RcdMilliAmps { get; set; } = 30;
    }
}
