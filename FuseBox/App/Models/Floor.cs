// Am i Join tp this rep?
using FuseBox.App.Models;

namespace FuseBox
{
    public class Floor : NonElectrical
    {
        // public int Power { get; set; } // Add Floor Power
        public List<Room> Rooms { get; set; } = new();
    }
}
