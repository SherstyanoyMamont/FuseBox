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