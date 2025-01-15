namespace FuseBox
{
    // Added a separate class of equipment
    public class Consumers
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Watt { get; set; }
        public bool Contactor { get; set; } // Connected to contactor?
        public bool SeparateRCD { get; set; } // Separate RCD?
        public bool IsCritical { get; set; } // Critical line (non-switchable)
    }
    public class Room
    {
        public string? Name { get; set; }
        public bool Area { get; set; } // "Dry" or "Wet"
        public int RoomPower { get; set; } // Add room Power
        public List<Consumers> Equipments { get; set; } // Load list
        public int CircuitBreakerRating { get; set; } // 2, 4, 6...
    }
    public class Floor
    {
        public string? FloorName { get; set; }
        public int FloorPower { get; set; } // Add Floor Power
        public List<Room> Rooms { get; set; } = new();
    }
}
