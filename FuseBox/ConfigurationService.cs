namespace FuseBox
{
    // Сервисный класс, который содержит логику для работы с объектами конфигурации
    public class ConfigurationService
    {
        // Создаем/Модифицируем наш проект
        public ProjectConfiguration GenerateConfiguration(ProjectConfiguration input)
        {
            ValidateInitialSettings(input.InitialSettings); // Проверка первичных данных

            // Расчет параметров устройства электрощита

            var shield = ConfigureShield(input.ShieldDevice, input.InitialSettings, input.Floors, input.GlobalGroupingParameters);

            // Группировка линий по этажам
            var floors = GroupFloors(input.Floors, input.FloorGrouping, input.GlobalGroupingParameters);

            return new ProjectConfiguration
            {
                InitialSettings = input.InitialSettings,
                ShieldDevice = shield,
                FloorGrouping = input.FloorGrouping,
                Floors = floors,
            };
        }

        private ShieldDevice ConfigureShield(ShieldDevice device, InitialSettings settings, List<Floor> floors, GlobalGroupingParameters globalGroupingParameters)
        {
            // Логика конфигурации устройств...

            // Настройки опцыонных автоматов \\

            // Проверяем наличие вводного автомата
            if (device.MainCircuitBreaker)
            {
                // Если Да то добавляем в список устройств щитка
                device.AddFuse(new Fuse(01, "MainCircuitBreaker", 2, settings.MainBreakerA, false));
            }

            // Проверяем наличие УЗПП
            if (device.SurgeProtectionKit)
            {
                device.AddFuse(new Fuse(02, "SurgeProtectionKit", 2, 100, false));
            }

            // Проверяем наличие модульный контактор
            if (device.ModularContactor)
            {
                device.AddFuse(new Fuse(03, "ModularContactor", 1, 25, false));
            }

            // Проверяем наличие реле напряжения
            if (device.VoltageRelay)
            {
                device.AddFuse(new Fuse(04, "VoltageRelay", 2, 100, false));
            }

            // Проверяем наличие розетки на DIN-рейку
            if (device.DinRailSocket)
            {
                device.AddFuse(new Fuse(05, "DinRailSocket", 1, 16, false));
            }

            // Проверяем наличие выключателя нагрузки
            if (device.LoadSwitch)
            {
                device.AddFuse(new Fuse(06, "LoadSwitch", 2, 25, false));
            }

            // Проверяем наличие счетчика на DIN-рейку
            if (device.DinRailMeter)
            {
                device.AddFuse(new Fuse(07, "DinRailMeter", 2, 10, false));
            }

            // Проверяем наличие противопожарного УЗО
            if (device.FireProtectionUZO)
            {
                device.AddFuse(new Fuse(08, "FireProtectionUZO", 2, 30, true)); // УЗО часто критическое
            }

            // Проверяем наличие неотключаемой линии
            if (device.NonDisconnectableLine)
            {
                device.AddFuse(new Fuse(09, "NonDisconnectableLine", 2, 25, true)); // Обязательно критическая линия
            }

            // Проверяем наличие кросс-блока
            if (device.CrossBlock)
            {
                device.AddFuse(new Fuse(10, "CrossBlock", 1, 0, false)); // Кросс-блок может быть без номинала
            }

            // Настройки автоматов для техники \\


            return device;
        }

        private void ValidateInitialSettings(InitialSettings settings)
        {
            if (settings.Phases != 1 && settings.Phases != 3)
                throw new ArgumentException("Invalid phase count");
            // Дополнительные проверки...
        }
        private List<Floor> GroupFloors(List<Floor> floors, FloorGrouping grouping, GlobalGroupingParameters globalGroupingParameters)
        {
            // Логика группировки этажей...
            return floors;
        }
    }
}
