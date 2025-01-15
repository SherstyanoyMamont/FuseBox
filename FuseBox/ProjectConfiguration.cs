namespace FuseBox
{
    public class ProjectConfiguration
    {
        public InitialSettings InitialSettings { get; set; }
        public ShieldDevice ShieldDevice { get; set; }
        public FloorGrouping FloorGrouping { get; set; }
        public GlobalGroupingParameters GlobalGroupingParameters { get; set; }
        public List<Floor> Floors { get; set; } = new();
        public List<Consumers> AllEquipments { get; set; }
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
