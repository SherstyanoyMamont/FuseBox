using FuseBox.App.Models.BaseAbstract;
using System.Text.Json.Serialization;

namespace FuseBox
{
    public class Project : BaseEntity
    {
        public FloorGrouping FloorGrouping { get; set; }
        public GlobalGrouping GlobalGrouping { get; set; }
        public InitialSettings InitialSettings { get; set; }
        public FuseBox FuseBox { get; set; }
        public List<Floor> Floors { get; set; } = new();
        public double TotalPower { get; set; } // A

        public Project()
        {
            InitialSettings = new InitialSettings();
            FuseBox = new FuseBox();
            FloorGrouping = new FloorGrouping();
            GlobalGrouping = new GlobalGrouping();
            Floors = new List<Floor>();
        }

        public Project(FuseBox fuseBox, FloorGrouping floorGrouping, GlobalGrouping globalGrouping, List<Floor> floors)     // Конструктор для тестов
        {
            FuseBox = fuseBox;
            FloorGrouping = floorGrouping;
            GlobalGrouping = globalGrouping;
            Floors = floors;
        }

        public Project(InitialSettings initialSettings, FloorGrouping floorGrouping, GlobalGrouping globalGrouping, List<Floor> floors)      // Конструктор для тестов
        {
            InitialSettings = initialSettings;
            FloorGrouping = floorGrouping;
            GlobalGrouping = globalGrouping;
            Floors = floors;
        }
        public Project(FuseBox fuseBox, InitialSettings initialSettings, FloorGrouping floorGrouping)      // Конструктор для тестов
        {
            FuseBox = fuseBox;
            InitialSettings = initialSettings;
            FloorGrouping = floorGrouping;
        }

        public double CalculateTotalPower() // Calculates the total power of the entire object
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Consumer)
                .Sum(equipment => equipment.Amper);
        }

        public int GetTotalNumberOfRooms() // Returns the total number of rooms in the project
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .Count();
        }
    }
}