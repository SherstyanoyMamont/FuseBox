// Am i Join tp this rep?
namespace FuseBox
{
    public class Floor : BaseEntity
    {
        public int Power { get; set; } // Add Floor Power
        public List<Room> Rooms { get; set; } = new();
    }
}
