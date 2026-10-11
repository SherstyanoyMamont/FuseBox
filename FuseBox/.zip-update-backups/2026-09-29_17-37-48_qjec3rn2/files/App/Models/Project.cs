using FuseBox.App.Models;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.FuseBox;
using System.Text.Json.Serialization;

namespace FuseBox
{
    public class Project : BaseEntity
    {
        public FloorGrouping FloorGrouping { get; set; }
        public GlobalGrouping GlobalGrouping { get; set; }
        public InitialSettings InitialSettings { get; set; }
        public FuseBoxUnit FuseBox { get; set; }
        public List<Floor> Floors { get; set; } = new();
        public double TotalPower { get; set; } // W

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        // Project ownership is assigned by the server.
        public int UserId { get; set; }

        [JsonIgnore]
        public User User { get; set; } = null!;

        public Project()
        {
            InitialSettings = new InitialSettings();
            FuseBox = new FuseBoxUnit();
            FloorGrouping = new FloorGrouping();
            GlobalGrouping = new GlobalGrouping();
            Floors = new List<Floor>();
        }

        public Project(
            FuseBoxUnit fuseBox,
            FloorGrouping floorGrouping,
            GlobalGrouping globalGrouping,
            List<Floor> floors)
        {
            FuseBox = fuseBox;
            FloorGrouping = floorGrouping;
            GlobalGrouping = globalGrouping;
            Floors = floors;
        }

        public Project(
            InitialSettings initialSettings,
            FloorGrouping floorGrouping,
            GlobalGrouping globalGrouping,
            List<Floor> floors)
        {
            InitialSettings = initialSettings;
            FloorGrouping = floorGrouping;
            GlobalGrouping = globalGrouping;
            Floors = floors;
        }

        public Project(
            FuseBoxUnit fuseBox,
            InitialSettings initialSettings,
            FloorGrouping floorGrouping)
        {
            FuseBox = fuseBox;
            InitialSettings = initialSettings;
            FloorGrouping = floorGrouping;
        }

        // Historical name: this currently returns total current (A), not watts.
        // Stage 8 will separate current and power explicitly.
        public double CalculateTotalPower()
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Consumer)
                .Sum(equipment => equipment.Amper);
        }

        public int GetTotalNumberOfRooms()
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .Count();
        }

        public decimal CalculateWireCrossSection()
        {
            decimal wireSection = 0;

            var copperWireTable = new Dictionary<double, double>
            {
                { 1.5, 18 },
                { 2.5, 25 },
                { 4, 32 },
                { 6, 40 },
                { 10, 63 },
                { 16, 80 }
            };

            // Preserve the existing selection behavior in stage 5.
            foreach (var wire in copperWireTable)
            {
                if (CalculateTotalPower() <= wire.Value)
                    wireSection = (decimal)wire.Key;
            }

            return wireSection;
        }
    }
}
