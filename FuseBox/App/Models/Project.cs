namespace FuseBox
{
    public class Project : BaseEntity
    {
        public FloorGrouping FloorGrouping { get; set; }
        public GlobalGrouping GlobalGrouping { get; set; }
        public InitialSettings InitialSettings { get; set; }
        public FuseBox FuseBox { get; set; }
        public List<Floor> Floors { get; set; } = new();
        public int TotalPower { get; set; }

        public Project()
        {
            InitialSettings = new InitialSettings();
            FuseBox = new FuseBox();
            FloorGrouping = new FloorGrouping();
            GlobalGrouping = new GlobalGrouping();
            Floors = new List<Floor>();
            TotalPower = CalculateTotalPower();
        }

        public int CalculateTotalPower() // Calculates the total power of the entire object
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Consumer)
                .Sum(equipment => equipment.Amper);
        }
    }
}