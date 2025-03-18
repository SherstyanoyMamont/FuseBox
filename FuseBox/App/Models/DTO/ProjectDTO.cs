using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.DTO.ConfugurationDTO;
using FuseBox.FuseBox;

namespace FuseBox.App.Models.DTO
{
    public class ProjectDTO : BaseEntity
    {
        public FloorGroupingDTO FloorGrouping { get; set; }
        public GlobalGroupingDTO GlobalGrouping { get; set; }
        public InitialSettingsDTO InitialSettings { get; set; }
        public FuseBoxUnitDTO FuseBox { get; set; }
        public List<FloorDTO> Floors { get; set; } = new();
        public double TotalPower { get; set; } // A

        public ProjectDTO()
        {
            InitialSettings = new InitialSettingsDTO();
            FuseBox = new FuseBoxUnitDTO();
            FloorGrouping = new FloorGroupingDTO();
            GlobalGrouping = new GlobalGroupingDTO();
            Floors = new List<FloorDTO>();
        }

        public ProjectDTO(FuseBoxUnitDTO fuseBox, FloorGroupingDTO floorGrouping, GlobalGroupingDTO globalGrouping, List<FloorDTO> floors)     // Конструктор для тестов
        {
            FuseBox = fuseBox;
            FloorGrouping = floorGrouping;
            GlobalGrouping = globalGrouping;
            Floors = floors;
        }

        public ProjectDTO(InitialSettingsDTO initialSettings, FloorGroupingDTO floorGrouping, GlobalGroupingDTO globalGrouping, List<FloorDTO> floors)      // Конструктор для тестов
        {
            InitialSettings = initialSettings;
            FloorGrouping = floorGrouping;
            GlobalGrouping = globalGrouping;
            Floors = floors;
        }
        public ProjectDTO(FuseBoxUnitDTO fuseBox, InitialSettingsDTO initialSettings, FloorGroupingDTO floorGrouping)      // Конструктор для тестов
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