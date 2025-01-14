namespace FuseBox
{
    // Added a separate class of equipment
    public class Equipment
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Watt { get; set; }
        public bool Contactor { get; set; } // Connected to contactor?
        public bool SeparateRCD { get; set; } // Separate RCD?
    }

    public class Room
    {
        public string? Name { get; set; }
        public string? ZoneType { get; set; } // "Dry" or "Wet"
        public int RoomPower { get; set; } // Add room Power
        public List<Equipment> Equipments { get; set; } // Load list
        public int CircuitBreakerRating { get; set; } // 2, 4, 6...
    }

    public class Floor
    {
        public string? FloorName { get; set; }
        public int FloorPower { get; set; } // Add Floor Power
        public List<Room> Rooms { get; set; } = new();
    }

    public class ProjectConfiguration
    {
        public InitialSettings InitialSettings { get; set; }
        public ShieldDevice ShieldDevice { get; set; }
        public FloorGrouping FloorGrouping { get; set; }
        public GlobalGroupingParameters GlobalGroupingParameters { get; set; }
        public List<Floor> Floors { get; set; } = new();
        public int TotalPower { get; set; }

        // Calculates the total power of the entire object
        public int CalculateTotalPower()
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Equipments)
                .Sum(equipment => equipment.Watt);
        }
    }
}
