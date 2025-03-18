using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using System.Text.Json.Serialization;

namespace FuseBox
{
    public class Room : BaseNonElectrical, IZone
    {
        // public int Rating { get; set; }                // ... 10А, 16А, 20А ...
        public List<Consumer> Consumer { get; set; } = new List<Consumer>();

        // Связь с этажом
        public int FloorId { get; set; }
        [JsonIgnore]
        public Floor? Floor { get; set; }

        public Room() { }

        public Room(List<Consumer> consumer)
        {
            Consumer = consumer;
        }
    }
}
