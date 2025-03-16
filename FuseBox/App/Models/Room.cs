using FuseBox.App.Interfaces;
using FuseBox.App.Models;

namespace FuseBox
{
    public class Room : NonElectrical, IZone
    {
        // public int Rating { get; set; }                // ... 10А, 16А, 20А ...
        public List<Consumer> Consumer { get; set; } = new List<Consumer>();

        // Связь с этажом
        public int FloorId { get; set; }

        public Room() { }

        public Room(List<Consumer> consumer)
        {
            Consumer = consumer;
        }
    }
}
