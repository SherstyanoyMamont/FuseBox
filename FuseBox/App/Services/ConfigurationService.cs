using System.Reflection;
using System.Reflection.Metadata;

namespace FuseBox
{
    // Сервисный класс, который содержит логику для работы с объектами конфигурации
    public class ConfigurationService
    {
        public int GetTotalPower(Project projectConfiguration)
        {
            // Вызываем метод CalculateTotalPower через объект projectConfiguration
            return projectConfiguration.CalculateTotalPower();
        }

        // Создаем/Модифицируем объект проекта
        public Project GenerateConfiguration(Project input) // Метод возвращает объект ProjectConfiguration
        {
            //ValidateInitialSettings(input.InitialSettings); // Проверка первичных данных***

            // Пример входных данных
            var project = new Project
            {
                Floors = new List<Floor>
                {
                    new Floor
                    {
                        FloorName = "Первый этаж",
                        Rooms = new List<Room>
                        {
                            new Room
                            {
                                Name = "Кухня",
                                Equipments = new List<Consumer>
                                {
                                    new Consumer { Name = "Духовой шкаф", Watt = 2200 },
                                    new Consumer { Name = "Варочная поверхность", Watt = 2000 }
                                }
                            },
                            new Room
                            {
                                Name = "Гостиная",
                                Equipments = new List<Consumer>
                                {
                                    new Consumer { Name = "lighting", Watt = 150 },
                                    new Consumer { Name = "socket", Watt = 300 }
                                }
                            }
                        }
                    },
                    new Floor
                    {
                        FloorName = "Второй этаж",
                        Rooms = new List<Room>
                        {
                            new Room
                            {
                                Name = "Спальня",
                                Equipments = new List<Consumer>
                                {
                                    new Consumer { Name = "lighting", Watt = 100 },
                                    new Consumer { Name = "socket", Watt = 200 }
                                }
                            }
                        }
                    }
                }
                
             };

            // Расчитывает общую мощность
            int totalPower = GetTotalPower(input);

            // Расчет параметров устройства электрощита
            var shield = ConfigureShield(input.Shield, input.InitialSettings, input.Floors, input.GlobalGrouping, totalPower, project);

            // То что мы возвращаем после модификаци или создания нового проекта
            return new Project
            {
                InitialSettings = input.InitialSettings,
                Shield = shield,
                FloorGrouping = input.FloorGrouping,
                Floors = floors,
                TotalPower = totalPower,
            };
        }

        // Логика конфигурации устройств...
        private Shield ConfigureShield(Shield shield, InitialSettings settings, List<Floor> floors, GlobalGrouping globalGroupingParameters, int totalPower, Project project)
        {
            // Создаем список всех потребителей
            List<Consumer> AllConsumers = new List<Consumer>();

            // Записываем все потребители в один список
            foreach (var floor in floors)
            {
                foreach (var room in floor.Rooms)
                {
                    foreach (var equipment in room.Equipments)
                    {
                        AllConsumers.Add(equipment);
                    }
                }
            }

            // Настройки опцыонных автоматов \\

            // Создаем список входной группы устройств
            List<IFuse> InputGroupOfModules = new List<IFuse>();

            // Проверяем наличие вводного автомата
            if (shield.MainCircuitBreaker)
            {
                // Если Да то добавляем во входную группу
                InputGroupOfModules.Add(new Module("Introductory", 63, 2, false, 35));
            }
            // Проверяем наличие УЗПП
            if (shield.SurgeProtectionKit)
            {
                InputGroupOfModules.Add(new Module("SPD", 100, 2, false, 65));
            }

            // Проверяем наличие выключателя 2P
            if (shield.LoadSwitch2P)
            {
                InputGroupOfModules.Add(new Module("LoadSwitch", 63, 2, false, 35));
            }

            // Проверяем наличие счетчика на DIN-рейку
            if (shield.DinRailMeter)
            {
                InputGroupOfModules.Add(new Module("DinRailMeter", 63 , 6, false, 145));
            }

            // Проверяем наличие противопожарного УЗО
            if (shield.FireProtectionUZO)
            {
                InputGroupOfModules.Add(new Module("RCDFire", 63, 2, false, 75)); // УЗО часто критическое
            }

            // Проверяем наличие модульный контактор
            //if (device.ModularContactor)
            //{
            //    device.AddFuse(new Fuse("ModularContactor", 25, 1, false, false, 84));
            //}

            // Проверяем наличие реле напряжения
            if (shield.VoltageRelay)
            {
                InputGroupOfModules.Add(new Module("VoltageRelay", 16 , 2, false, 40));
            }

            // Проверяем наличие розетки на DIN-рейку
            if (shield.DinRailSocket)
            {
                InputGroupOfModules.Add(new Module("DinRailSocket", 16, 3, false, 22));
            }

            // Проверяем наличие УЗО неотключаемой линии
            if (shield.NonDisconnectableLine)
            {
                InputGroupOfModules.Add(new Module("NonDisconnectableLine", 25, 2, false, 43)); // Обязательно критическая линия
            }

            // Проверяем наличие общего выключателя
            if (shield.LoadSwitch)
            {
                InputGroupOfModules.Add(new Module("LoadSwitch", 63, 2, false, 35));
            }

            // Проверяем наличие кросс-модуля
            if (shield.CrossModule)
            {
                InputGroupOfModules.Add(new Module("CrossBlock", 100, 4, false, 25)); // Кросс-блок может быть без номинала
            }

            // Настройки автоматов для техники \\
            // Примерная мощность автомата С16 - 3.6 кВт.

            // Примерная мощность УЗО      10А - 2.2 кВт.
            // Примерная мощность УЗО      32А - 7 кВт.
            // Примерная мощность УЗО      63А - 13,9 кВт. 

            // Типы УЗО на 10-300 мА ()	10 А	16 А	25 А	32 А	40 А	64 А	80 А	100 А
            // <30 мА – для защиты человека, 100> мА – для защиты зданий от пожаров

            // 118 A

            decimal totalAmper = totalPower / settings.VoltageStandard; // A

            // Создаем список всех АВ автоматов для оборудовния
            List<Fuse> AVFuses = new List<Fuse>();

            // Создаем список для заполнения в правильном порядке
            List<IFuse> Fuses = new List<IFuse>();

            int countAVFuses = AVFuses.Count;

            // Автоматы с учетом сортировки: Свет, Розетки, Кондиционеры
            if (AllConsumers.Any(e => e.Name.Equals("lighting", StringComparison.OrdinalIgnoreCase))) 
            {
                for (int i = 0; i < globalGroupingParameters.Lighting; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < globalGroupingParameters.Sockets; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("air conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < globalGroupingParameters.Conditioners; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10));
                }
            }

            // Добавляем втоматы без сортировки
            foreach (var Equipment in AllConsumers)
            {
                if (Equipment.Name != "lighting" && Equipment.Name != "socket" && Equipment.Name != "air conditioner") 
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10));
                }
            }

            int avIndex = 0;

            // Логика распределения УЗО от нагрузки
            if (totalAmper <= 16)
            {
                // Создаем УЗО
                Fuses.Add(new Module("RCD", 16, 2, false, 43));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    Fuses.Add(fuse);
                }
            }
            else if (totalAmper > 16 && totalAmper <= 32)
            {
                Fuses.Add(new Module("RCD", 32, 2, false, 43));

                foreach (var fuse in AVFuses)
                {
                    Fuses.Add(fuse);
                }
            }
            else if (totalAmper > 32 && totalAmper <= 63)
            {
                Fuses.Add(new Module("RCD", 63, 2, false, 43));

                foreach (var fuse in AVFuses)
                {
                    Fuses.Add(fuse);
                }
            }
            else
            {
                double countOfRCD = Math.Ceiling(totalPower / 63.00);
                int countABPerRCD = Convert.ToInt32(Math.Ceiling(countAVFuses / countOfRCD));

                for (int i = 0; i < countOfRCD; i++)
                {
                    Fuses.Add(new Module("RCD", 63, 2, false, 43));

                    // Добавляем созданые ранее АВ автоматы в список
                    for (int ii = 0; ii < countABPerRCD && avIndex < countABPerRCD; ii++) // !!!
                    {
                        Fuses.Add(AVFuses[avIndex]);

                        // Обновляем индекс
                        avIndex++;
                    }
                }
            }

            shield.Fuses.Add(InputGroupOfModules);
            shield.Fuses.Add(Fuses);

            return shield;
        }

        // Проверка первичных данных***
        //private void ValidateInitialSettings(InitialSettings settings)
        //{
        //    if (settings.Phases != 1 && settings.Phases != 3)
        //        throw new ArgumentException("Invalid phase count");
        //    // Дополнительные проверки...
        //}
    }
}
