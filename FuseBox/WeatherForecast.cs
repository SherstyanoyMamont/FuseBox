// Am i Join tp this rep?
namespace FuseBox
{
    public class InitialSettings
    {
        public int Phases { get; set; } // 1 или 3
        public int MainCircuitBreaker { get; set; } // 25А, 32А и т.д.
        public int ShieldWidth { get; set; } // 12, 16, 18
        public int VoltageStandard { get; set; } // 220 или 230
    }

    public class ShieldDevice
    {
        public bool SurgeProtectionKit { get; set; }
        public bool MainCircuitBreaker { get; set; }
        public bool ModularContactor { get; set; }
        public string VoltageRelay { get; set; } // "Один" или "Три"
        public bool DinRailSocket { get; set; }
        public bool LoadSwitch { get; set; }
        public bool DinRailMeter { get; set; }
        public bool FireProtectionUZO { get; set; }
        public bool NonDisconnectableLine { get; set; }
        public bool CrossBlock { get; set; }
    }

    public class FloorGrouping
    {
        public bool IndividualFloorGrouping { get; set; }
        public bool SeparateUZOPerFloor { get; set; }
    }

    public class Room
    {
        public string Name { get; set; }
        public string ZoneType { get; set; } // "Сухая" или "Мокрая"
        public List<string> LoadTypes { get; set; } // Список нагрузки
        public int CircuitBreakerRating { get; set; } // 2, 4, 6 и т.д.
    }

    public class Floor
    {
        public string FloorName { get; set; }
        public List<Room> Rooms { get; set; } = new();
    }

    public class ProjectConfiguration
    {
        public InitialSettings InitialSettings { get; set; }
        public ShieldDevice ShieldDevice { get; set; }
        public FloorGrouping FloorGrouping { get; set; }
        public List<Floor> Floors { get; set; } = new();
    }
    /// <summary>
    /// /////////////////////////////////////////////////////////////////////////////
    /// </summary>

    public class ConfigurationService
    {
        public ProjectConfiguration GenerateConfiguration(ProjectConfiguration input)
        {
            // Проверка первичных данных
            ValidateInitialSettings(input.InitialSettings);

            // Расчет параметров устройства электрощита
            var shield = ConfigureShield(input.ShieldDevice, input.InitialSettings);

            // Группировка линий по этажам
            var floors = GroupFloors(input.Floors, input.FloorGrouping);

            return new ProjectConfiguration
            {
                InitialSettings = input.InitialSettings,
                ShieldDevice = shield,
                FloorGrouping = input.FloorGrouping,
                Floors = floors
            };
        }

        private void ValidateInitialSettings(InitialSettings settings)
        {
            if (settings.Phases != 1 && settings.Phases != 3)
                throw new ArgumentException("Invalid phase count");
            // Дополнительные проверки...
        }

        private ShieldDevice ConfigureShield(ShieldDevice device, InitialSettings settings)
        {
            // Логика конфигурации устройств...
            return device;
        }

        private List<Floor> GroupFloors(List<Floor> floors, FloorGrouping grouping)
        {
            // Логика группировки этажей...
            return floors;
        }
    }
    //public class WeatherForecast
    //{
    //    public DateOnly Date { get; set; }

    //    public int TemperatureC { get; set; }

    //    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    //    public string? Summary { get; set; }
    //}
}
