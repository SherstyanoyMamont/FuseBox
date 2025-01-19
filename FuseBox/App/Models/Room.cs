// Am i Join tp this rep?
namespace FuseBox
{
    public class Room
    {
        public string? Name { get; set; }
        public bool Area { get; set; } // "Dry" or "Wet"
        public int RoomPower { get; set; } // Add room Power
        public List<Consumer> Equipments { get; set; } // Load list
        public int CircuitBreakerRating { get; set; } // 2, 4, 6...
    }
}
