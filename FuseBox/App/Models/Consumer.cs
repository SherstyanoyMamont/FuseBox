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
        public Room? Room { get; set; }

        //// Связь с FuseBoxUnit
        //public int FuseBoxUnitId { get; set; }
        //public FuseBoxUnit? FuseBoxUnit { get; set; }

        public Consumer() { }
        public Consumer(string name, int maxLoad)        // Для тестов
        {
            this.Name = name;
            this.Amper = maxLoad;
        }
    }
}
