namespace FuseBox
{
    // Сервисный класс, который содержит логику для работы с объектами конфигурации
    public class ConfigurationService
    {
        public int GetTotalPower(ProjectConfiguration projectConfiguration)
        {
            // Вызываем метод CalculateTotalPower через объект projectConfiguration
            return projectConfiguration.CalculateTotalPower();
        }

        // Создаем/Модифицируем объект проекта
        public ProjectConfiguration GenerateConfiguration(ProjectConfiguration input) // Метод возвращает объект ProjectConfiguration
        {
            ValidateInitialSettings(input.InitialSettings); // Проверка первичных данных

            // Расчитывает общую мощность
            int totalPower = GetTotalPower(input);

            // Расчет параметров устройства электрощита
            var shield = ConfigureShield(input.ShieldDevice, input.InitialSettings, input.Floors, input.GlobalGroupingParameters, totalPower, input.AllEquipments);

            // Группировка линий по этажам
            var floors = GroupFloors(input.Floors, input.FloorGrouping, input.GlobalGroupingParameters);

            // То что мы возвращаем после модификаци или создания нового проекта
            return new ProjectConfiguration
            {
                InitialSettings = input.InitialSettings,
                ShieldDevice = shield,
                FloorGrouping = input.FloorGrouping,
                Floors = floors,
                TotalPower = totalPower,
            };
        }

        // Логика конфигурации устройств...
        private ShieldDevice ConfigureShield(ShieldDevice device, InitialSettings settings, List<Floor> floors, GlobalGroupingParameters globalGroupingParameters, int totalPower, List<Consumers> allEquipments)
        {
            // Настройки опцыонных автоматов \\

            // Проверяем наличие вводного автомата
            if (device.MainCircuitBreaker)
            {
                // Если Да то добавляем в список устройств щитка
                device.AddFuse(new SimpleFuse("Introductory", 63, 2, false, 35));
            }
            // Проверяем наличие УЗПП
            if (device.SurgeProtectionKit)
            {
                device.AddFuse(new SimpleFuse("SPD", 100, 2, false, 65));
            }

            // Проверяем наличие выключателя 2P
            if (device.LoadSwitch2P)
            {
                // Если Да то добавляем в список устройств щитка
                device.AddFuse(new SimpleFuse("LoadSwitch", 63, 2, false, 35));
            }

            // Проверяем наличие счетчика на DIN-рейку
            if (device.DinRailMeter)
            {
                device.AddFuse(new SimpleFuse("DinRailMeter", 63 , 6, false, 145));
            }

            // Проверяем наличие противопожарного УЗО
            if (device.FireProtectionUZO)
            {
                device.AddFuse(new SimpleFuse("RCDFire", 63, 2, false, 75)); // УЗО часто критическое
            }

            // Проверяем наличие модульный контактор
            //if (device.ModularContactor)
            //{
            //    device.AddFuse(new Fuse("ModularContactor", 25, 1, false, false, 84));
            //}

            // Проверяем наличие реле напряжения
            if (device.VoltageRelay)
            {
                device.AddFuse(new SimpleFuse("VoltageRelay", 16 , 2, false, 40));
            }

            // Проверяем наличие розетки на DIN-рейку
            if (device.DinRailSocket)
            {
                device.AddFuse(new SimpleFuse("DinRailSocket", 16, 3, false, 22));
            }

            // Проверяем наличие УЗО неотключаемой линии
            if (device.NonDisconnectableLine)
            {
                device.AddFuse(new SimpleFuse("NonDisconnectableLine", 25, 2, false, 43)); // Обязательно критическая линия
            }

            // Проверяем наличие общего выключателя
            if (device.LoadSwitch)
            {
                // Если Да то добавляем в список устройств щитка
                device.AddFuse(new SimpleFuse("LoadSwitch", 63, 2, false, 35));
            }

            // Проверяем наличие кросс-модуля
            if (device.CrossModule)
            {
                device.AddFuse(new SimpleFuse("CrossBlock", 100, 4, false, 25)); // Кросс-блок может быть без номинала
            }

            // Настройки автоматов для техники \\
            // Примерная мощность автомата С16 - 3.6 кВт.

            // Примерная мощность УЗО      10А - 2.2 кВт.
            // Примерная мощность УЗО      32А - 7 кВт.
            // Примерная мощность УЗО      63А - 13,9 кВт. 

            // Типы УЗО на 10-300 мА ()	10 А	16 А	25 А	32 А	40 А	64 А	80 А	100 А
            // <30 мА – для защиты человека, 100> мА – для защиты зданий от пожаров

            // 118 A

            decimal totalAmper = totalPower / settings.VoltageStandard; // A !!!

            // Создаем список всех АВ автоматов для оборудовния
            List<SimpleFuse> AVFuses = new List<SimpleFuse>();

            int countAVFuses = AVFuses.Count;

            // Автоматы с учетом сортировки: Свет, Розетки, Кондиционеры
            if (allEquipments.Any(e => e.Name.Equals("lighting", StringComparison.OrdinalIgnoreCase))) 
            {
                for (int i = 0; i < globalGroupingParameters.Lighting; i++)
                {
                    AVFuses.Add(new SimpleFuse("AV", 16, 1, false, 10));
                }
            }
            if (allEquipments.Any(e => e.Name.Equals("socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < globalGroupingParameters.Sockets; i++)
                {
                    AVFuses.Add(new SimpleFuse("AV", 16, 1, false, 10));
                }
            }
            if (allEquipments.Any(e => e.Name.Equals("air conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < globalGroupingParameters.Conditioners; i++)
                {
                    AVFuses.Add(new SimpleFuse("AV", 16, 1, false, 10));
                }
            }

            // Добавляем втоматы без сортировки
            foreach (var Equipment in allEquipments)
            {
                if (Equipment.Name != "lighting" && Equipment.Name != "socket" && Equipment.Name != "air conditioner") 
                {
                    AVFuses.Add(new SimpleFuse(Equipment.Name, 16, 1, false, 10));
                }
            }

            int avIndex = 0;

            // Логика распределения УЗО от нагрузки
            if (totalAmper <= 16)
            {
                // Создаем УЗО
                device.AddFuse(new SimpleFuse("RCD", 16, 2, false, 43));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    device.AddFuse(fuse);
                }
            }
            else if (totalAmper > 16 && totalAmper <= 32)
            {
                device.AddFuse(new SimpleFuse("RCD", 32, 2, false, 43));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    device.AddFuse(fuse);
                }
            }
            else if (totalAmper > 32 && totalAmper <= 63)
            {
                device.AddFuse(new SimpleFuse("RCD", 63, 2, false, 43));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    device.AddFuse(fuse);
                }
            }
            else
            {
                double countOfRCD = Math.Ceiling(totalPower / 63.00);
                int countABPerRCD = Convert.ToInt32(Math.Ceiling(countAVFuses / countOfRCD));

                for (int i = 0; i < countOfRCD; i++)
                {
                    device.AddFuse(new SimpleFuse("RCD", 63, 2, false, 43));

                    // Добавляем созданые ранее АВ автоматы в список
                    for (int ii = 0; ii < countABPerRCD && avIndex < countABPerRCD; ii++) // !!!
                    {
                        device.AddFuse(AVFuses[avIndex]);

                        // Обновляем индекс
                        avIndex++;
                    }
                }

            }




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
