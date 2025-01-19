// Am i Join tp this rep?
namespace FuseBox
{
    public class Floor
    {
        public string? FloorName { get; set; }
        public int FloorPower { get; set; } // Add Floor Power
        public List<Room> Rooms { get; set; } = new();
    }
}
