using System.Reflection;
using System.Reflection.Metadata;

namespace FuseBox
{
    // Сервисный класс, который содержит логику для работы с объектами конфигурации
    public class ConfigurationService
    {
        // Создаем/Модифицируем объект проекта
        public Project GenerateConfiguration(Project input) // Метод возвращает объект ProjectConfiguration
        {
            //ValidateInitialSettings(input.InitialSettings); // Проверка первичных данных***

            // Создаем список всех потребителей и записываем их
            List<Consumer> AllConsumers = CalculateAllConsumers(input);

            // Список не отключаемых устройств
            List<Consumer> CriticalLine = new List<Consumer>(AllConsumers.FindAll(e => e.IsCritical == true));

            decimal totalAmper = input.CalculateTotalPower() / input.InitialSettings.VoltageStandard; // A

            // Расчет параметров устройства электрощита
            Shield shield;

            if (input.InitialSettings.PhasesCount == 1) // Колличество фаз
            {
                shield = ConfigureShield(input, AllConsumers, CriticalLine, totalAmper);
            }
            else
            {
                shield = ConfigureShield3(input, AllConsumers, CriticalLine, totalAmper);
            }

            // Возвращаем новый модифицированный объект
            return new Project
            {
                InitialSettings = input.InitialSettings,
                GlobalGrouping = input.GlobalGrouping,
                Shield = shield, // Возвращаем настроенный щит, а все остальное так же
                FloorGrouping = input.FloorGrouping,
                Floors = input.Floors,
            };
        }

        // Логика конфигурации устройств...
        private Shield ConfigureShield(Project project, List<Consumer> AllConsumers, List<Consumer> CriticalLine, decimal totalAmper)
        {
            // Создаем список всех АВ автоматов для оборудовния
            List<Fuse> AVFuses = new List<Fuse>();

            // Делаем единый список модулей в Щите
            List<Component> shieldModuleSet = new List<Component>();


            if (project.Shield.MainBreaker)
            {
                shieldModuleSet.Add(new Introductory("Introductory", project.InitialSettings.MainAmperage, 2, false, 35, false, false));
            }

            if (project.Shield.SurgeProtection)
            {
                shieldModuleSet.Add(new Module("SPD", 100, 2, false, 65, false, "class 2"));
            }

            if (project.Shield.LoadSwitch2P)
            {
                shieldModuleSet.Add(new Module("LoadSwitch", 63, 2, false, 35, false, "2P"));
            }

            if (project.Shield.RailMeter)
            {
                shieldModuleSet.Add(new Module("DinRailMeter", 63, 6, false, 145, false, ""));
            }

            if (project.Shield.FireUZO)
            {
                shieldModuleSet.Add(new RCDFire("RCDFire", 63, 2, false, 75, 300, false)); // УЗО часто критическое
            }

            if (project.Shield.VoltageRelay)
            {
                shieldModuleSet.Add(new Module("VoltageRelay", 16, 2, false, 40, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));
            }

            if (project.Shield.RailSocket)
            {
                shieldModuleSet.Add(new Module("DinRailSocket", 16, 3, false, 22, false, "UK"));
            }

            if (project.Shield.NDisconnectableLine)
            {
                shieldModuleSet.Add(new RCDNonS("NonDisconnectableLine", 25, 2, false, 43, 30, CriticalLine, false)); // Критическая линия
            }

            if (project.Shield.LoadSwitch)
            {
                shieldModuleSet.Add(new Module("LoadSwitch", 63, 2, false, 35, false, "class 2"));
            }

            if (project.Shield.ModularContactor)
            {
                shieldModuleSet.Add(new Contactor());
            }

            if (project.Shield.CrossModule)
            {
                shieldModuleSet.Add(new Module("CrossBlock", 100, 4, false, 25, false, "class 2")); // Кросс-блок может быть без номинала
            }

            // Настройки автоматов для техники \\
            // Примерная мощность автомата С16 - 3.6 кВт.

            // Примерная мощность УЗО      10А - 2.2 кВт.
            // Примерная мощность УЗО      32А - 7 кВт.
            // Примерная мощность УЗО      63А - 13,9 кВт. 

            // Типы УЗО на 10-300 мА ()	10 А	16 А	25 А	32 А	40 А	64 А	80 А	100 А
            // <30 мА – для защиты человека, 100> мА – для защиты зданий от пожаров

            // 118 A


            // Автоматы с учетом сортировки: Свет, Розетки, Кондиционеры
            if (AllConsumers.Any(e => e.Name.Equals("lighting", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Lighting; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Sockets; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("air conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("heated floor", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }

            foreach (var consumer in AllConsumers) // Добавляем втоматы без сортировки
            {

                if (consumer.Name != "lighting" && consumer.Name != "socket" && consumer.Name != "air conditioner")
                {
                    //List<Consumer> Consumers = new List<Consumer>();
                    //Consumers.Add(consumer);
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false)); // Consumers
                }
            }

            int avIndex = 0;
            // int AVCount = AVFuses.Count - 1; // !!! -1 Это Костыль

            // Логика распределения УЗО от нагрузки
            if (totalAmper <= 8)
            {
                // Создаем УЗО
                shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else if (totalAmper > 8 && totalAmper <= 16)
            {
                shieldModuleSet.Add(new RCD("RCD", 32, 2, false, 43, 2, false));

                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else if (totalAmper > 16 && totalAmper <= 32)
            {
                shieldModuleSet.Add(new RCD("RCD", 63, 2, false, 43, 2, false));

                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else
            {
                double countOfRCD = Math.Ceiling(project.CalculateTotalPower() / project.InitialSettings.VoltageStandard / 30.00);

                int countABPerRCD = Convert.ToInt32(Math.Ceiling(AVFuses.Count / countOfRCD));

                for (int i = 0; i < countOfRCD; i++)
                {
                    shieldModuleSet.Add(new RCD("RCD", 63, 2, false, 43, 2, false));

                    // Добавляем созданые ранее АВ автоматы в список
                    for (int ii = 0; ii < countABPerRCD && avIndex < AVFuses.Count - 1; ii++) // !!!
                    {
                        shieldModuleSet.Add(AVFuses[avIndex]);
                        avIndex++;
                    }
                }
            }

            // Компоновка Щита по уровням...
            ShieldByLevel(project, shieldModuleSet);

            return project.Shield;
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        private Shield ConfigureShield3(Project project, List<Consumer> AllConsumers, List<Consumer> CriticalLine, decimal totalAmper)
        {

            // Создаем список всех АВ автоматов для оборудовния
            List<Fuse> AVFuses = new List<Fuse>();

            // Делаем единый список модулей в Щите
            List<Component> shieldModuleSet = new List<Component>();

            // Настройки опцыонных автоматов \\
            if (project.Shield.MainBreaker)
            {
                // Если да, то добавляем 3 фазы + ноль
                if (project.Shield.Main3PN)
                {
                    shieldModuleSet.Add(new Introductory("Introductory 3P+N", project.InitialSettings.MainAmperage, 2, false, 35, false, false));
                }
                else
                {
                    shieldModuleSet.Add(new Introductory("Introductory 3P", project.InitialSettings.MainAmperage, 2, false, 35, false, false));
                }
            }

            if (project.Shield.SurgeProtection)
            {
                shieldModuleSet.Add(new Module("SPD", 100, 2, false, 65, false, "class 2"));
            }

            if (project.Shield.RailMeter)
            {
                shieldModuleSet.Add(new Module("DinRailMeter", 63, 6, false, 145, false, ""));
            }

            if (project.Shield.FireUZO)
            {
                shieldModuleSet.Add(new RCDFire("RCDFire", 63, 2, false, 75, 300, false)); // УЗО часто критическое
            }

            if (project.Shield.ModularContactor)
            {
                shieldModuleSet.Add(new Contactor());
                shieldModuleSet.Add(new Contactor());
            }

            // Проверяем наличие реле напряжения
            if (project.Shield.VoltageRelay)
            {
                if (project.Shield.ThreePRelay)
                {
                    shieldModuleSet.Add(new Module("Three-Phase Relay", 16, 6, false, 60, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));

                }
                else
                {
                    shieldModuleSet.Add(new Module("VoltageRelay", 16, 2, false, 40, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));
                    shieldModuleSet.Add(new Module("VoltageRelay", 16, 2, false, 40, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));
                    shieldModuleSet.Add(new Module("VoltageRelay", 16, 2, false, 40, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));
                }
            }

            // Проверяем наличие розетки на DIN-рейку
            if (project.Shield.RailSocket)
            {
                shieldModuleSet.Add(new Module("DinRailSocket", 16, 3, false, 22, false, "UK"));
            }

            // Проверяем наличие общего выключателя
            if (project.Shield.LoadSwitch)
            {
                shieldModuleSet.Add(new Module("LoadSwitch", 63, 2, false, 35, false, "class 2"));
            }

            // Проверяем наличие кросс-модуля
            if (project.Shield.CrossModule)
            {
                shieldModuleSet.Add(new Module("CrossBlock", 100, 4, false, 25, false, "class 2")); // Кросс-блок может быть без номинала
            }

            // Настройки автоматов для техники \\
            // Автоматы с учетом сортировки: Свет, Розетки, Кондиционеры
            if (AllConsumers.Any(e => e.Name.Equals("lighting", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Lighting; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Sockets; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("air conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("heated floor", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }

            // Добавляем втоматы без сортировки
            foreach (var Equipment in AllConsumers)
            {
                if (Equipment.Name != "lighting" && Equipment.Name != "socket" && Equipment.Name != "air conditioner")
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
                }
            }

            int avIndex = 0;
            //int AVCount = AVFuses.Count - 1; // !!! -1 Это Костыль

            // Логика распределения УЗО от нагрузки
            if (totalAmper <= 8)
            {
                // Создаем УЗО
                shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else if (totalAmper > 8 && totalAmper <= 16)
            {
                shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else if (totalAmper > 16 && totalAmper <= 32)
            {
                shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else
            {
                double countOfRCD = Math.Ceiling(project.CalculateTotalPower() / project.InitialSettings.VoltageStandard / 30.00);

                int countABPerRCD = Convert.ToInt32(Math.Ceiling(AVFuses.Count / countOfRCD));

                for (int i = 0; i < countOfRCD; i++)
                {
                    shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

                    // Добавляем созданые ранее АВ автоматы в список
                    for (int ii = 0; ii < countABPerRCD && avIndex < AVFuses.Count - 1; ii++) // !!!
                    {
                        shieldModuleSet.Add(AVFuses[avIndex]);
                        avIndex++;
                    }
                }
            }

            // Компоновка Щита по уровням...
            ShieldByLevel(project, shieldModuleSet);

            return project.Shield;
        }

        // Логика распределения модулей по уровням...
        public void ShieldByLevel(Project project, List<Component> shieldModuleSet)
        {

            double countOfSlots = 0;
            // Вычисляем общее количество слотов для Щитовой панели
            for (int i = 0; i < shieldModuleSet.Count; i++)
                countOfSlots += shieldModuleSet[i].Slots;

            var countOfDINLevels = Math.Ceiling(countOfSlots / project.InitialSettings.ShieldWidth); //Количество уровней ДИН рейки в Щите

            // Инициализируем списки каждого уровня щита по ПЕРВИЧНЫМ ДАННЫМ (без учёта потенциальных пустых мест)
            for (int i = 0; i < countOfDINLevels; i++)
                project.Shield.Fuses.Add(new List<Component>());

            project.Shield.DINLines = (int)countOfDINLevels; // Запись в поле объекта количество уровней в щите (Как по мне лишнее)
            int occupiedSlots = 0;
            int currentLevel = 0;
            int shieldWidth = project.InitialSettings.ShieldWidth;

            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                if (currentLevel >= project.Shield.Fuses.Count)
                    project.Shield.Fuses.Add(new List<Component>()); // Добавляем новый уровень, если его ещё нет

                if (shieldModuleSet[i].Name == "RCD") // i-й элемент - УЗО, значит дальше автоматы, с ними связанные                
                    IsRCDBlockFitAtLevel(project, shieldModuleSet, ref i, ref occupiedSlots, ref currentLevel, shieldWidth);

                else // i-й элемент - другой модуль, значит применяется обычная логика                
                    IsModuleFitAtLevel(project, shieldModuleSet, ref i, ref occupiedSlots, ref currentLevel, shieldWidth);

                if (occupiedSlots < shieldWidth && i == shieldModuleSet.Count - 1)
                    project.Shield.Fuses[currentLevel].Add(new Module("{empty space}", 0, shieldWidth - occupiedSlots, false, 0, false, "empty space"));
            }
        }

        public static void IsRCDBlockFitAtLevel(Project project, List<Component> shieldModuleSet, ref int i, ref int occupiedSlots, ref int currentLevel, int shieldWidth)
        {
            double rcdBlockSlots = shieldModuleSet[i].Slots;
            int j = i + 1;
            while (j < shieldModuleSet.Count && shieldModuleSet[j].Name?.StartsWith("AV") == true)
            {
                rcdBlockSlots += shieldModuleSet[j].Slots;
                j++;
            }

            if (occupiedSlots + rcdBlockSlots > shieldWidth)
            {
                if (occupiedSlots < shieldWidth)
                    project.Shield.Fuses[currentLevel].Add(new Module("{empty space}", 0, shieldWidth - occupiedSlots, false, 0, false, "empty space")); // Добавлена проверка на добавление доп. уровня ниже

                occupiedSlots = 0;
                currentLevel++;
                if (currentLevel >= project.Shield.Fuses.Count)
                    project.Shield.Fuses.Add(new List<Component>()); // Добавляем новый уровень, если его ещё нет                

                for (int k = i; k < j; k++)
                    project.Shield.Fuses[currentLevel].Add(shieldModuleSet[k]);

                occupiedSlots += (int)rcdBlockSlots;
            }
            else
            {
                occupiedSlots += (int)rcdBlockSlots;
                for (int k = i; k < j; k++)
                    project.Shield.Fuses[currentLevel].Add(shieldModuleSet[k]);
            }
            i = j - 1; // Пропускаем обработанные AV
        }
        public static void IsModuleFitAtLevel(Project project, List<Component> shieldModuleSet, ref int i, ref int occupiedSlots, ref int currentLevel, int shieldWidth)
        {
            occupiedSlots += (int)shieldModuleSet[i].Slots;
            if (occupiedSlots < shieldWidth)                // место есть как для модуля, так и после него на уровне
            {
                project.Shield.Fuses[currentLevel].Add(shieldModuleSet[i]);
            }
            else if (occupiedSlots > shieldWidth)           // модуль не помещается на уровне. Проверку и добавление уровней делать не надо!
            {
                project.Shield.Fuses[currentLevel].Add(new Module("{empty space}", 0, shieldWidth - (occupiedSlots - (int)shieldModuleSet[i].Slots), false, 0, false, "empty space"));
                currentLevel++;
                occupiedSlots = (int)shieldModuleSet[i].Slots;
                project.Shield.Fuses[currentLevel].Add(shieldModuleSet[i]);
            }
            else // Слотов на уровне аккурат равно длине шины
            {
                project.Shield.Fuses[currentLevel].Add(shieldModuleSet[i]);
                currentLevel++;
                occupiedSlots = 0;
            }
        }


        public List<Consumer> CalculateAllConsumers(Project project)
        {
            List<Consumer> AllConsumers = new List<Consumer>();
            foreach (var floor in project.Floors)
            {
                foreach (var room in floor.Rooms)
                {
                    foreach (var equipment in room.Consumer)
                    {
                        AllConsumers.Add(equipment);
                    }
                }
            }
            return AllConsumers;
        }
    }

    //Проверка первичных данных***
    //private void ValidateInitialSettings(InitialSettings settings)
    //{
    //    if (settings.Phases != 1 && settings.Phases != 3)
    //        throw new ArgumentException("Invalid phase count");
    //    // Дополнительные проверки...
    //}

    //PlantUML: @startuml

    //PlantUML: @enduml
}




//{
//    "floorGrouping": {
//        "FloorGroupingP": true,
//        "separateUZO": true
//    },
//  "globalGrouping": {
//        "Sockets": 1,
//    "Lighting": 1,
//    "Conditioners": 1
//  },
//  "initialSettings": {
//        "PhaseCount": 1,
//    "MainAmperage": 25,
//    "ShieldWidth": 16,
//    "VoltageStandard": 220,
//    "PowerCoefficient": 1
//  },
//  "shield": {
//        "MainBreaker": true,
//    "Main3PN": false,
//    "SurgeProtection": true,
//    "LoadSwitch2P": true,
//    "ModularContactor": true,
//    "RailMeter": true,
//    "FireUZO": true,
//    "VoltageRelay": true,
//    "RailSocket": true,
//    "NDisconnectableLine": true,
//    "LoadSwitch": true,
//    "CrossModule": true,
//    "DINLines": 1
//  },
//    "floors": [
//      {
//        "floorName": "Ground Floor",
//      "rooms": [
//        {
//            "name": "Living Room",
//          "area": true,
//          "rating": 5,
//          "Consumer": [
//            {
//                "Id": 1,
//              "name": "TV",
//              "watt": 150,
//              "contactor": false,
//              "separateRCD": false,
//              "isCritical": false
//            },
//            {
//                "Id": 2,
//              "name": "Air Conditioner",
//              "watt": 2000,
//              "contactor": true,
//              "separateRCD": true,
//              "isCritical": true
//            },
//            {
//                "Id": 3,
//              "name": "Lighting",
//              "watt": 300,
//              "contactor": false,
//              "separateRCD": false,
//              "isCritical": false
//            }
//          ],
//          "tPower": 2450
//        },
//        {
//            "name": "Kitchen",
//          "area": true,
//          "rating": 4,
//          "Consumer": [
//            {
//                "Id": 4,
//              "name": "Refrigerator",
//              "watt": 800,
//              "contactor": false,
//              "separateRCD": true,
//              "isCritical": true
//            },
//            {
//                "Id": 5,
//              "name": "Microwave",
//              "watt": 1200,
//              "contactor": false,
//              "separateRCD": true,
//              "isCritical": false
//            },
//            {
//                "Id": 6,
//              "name": "Oven",
//              "watt": 2500,
//              "contactor": true,
//              "separateRCD": true,
//              "isCritical": true
//            }
//          ],
//          "tPower": 4500
//        }
//      ]
//    },
//    {
//        "floorName": "First Floor",
//      "rooms": [
//        {
//            "name": "Bedroom 1",
//          "area": true,
//          "rating": 3,
//          "Consumer": [
//            {
//                "id": 7,
//              "name": "Heater",
//              "watt": 1000,
//              "contactor": false,
//              "separateRCD": false,
//              "isCritical": true
//            },
//            {
//                "id": 8,
//              "name": "Fan",
//              "watt": 100,
//              "contactor": false,
//              "separateRCD": false,
//              "isCritical": false
//            }
//          ],
//          "tPower": 1100
//        },
//        {
//            "name": "Bathroom",
//          "area": false,
//          "rating": 4,
//          "Consumer": [
//            {
//                "id": 9,
//              "name": "Water Heater",
//              "watt": 3000,
//              "contactor": true,
//              "separateRCD": true,
//              "isCritical": true
//            },
//            {
//                "id": 10,
//              "name": "Hair Dryer",
//              "watt": 1500,
//              "contactor": false,
//              "separateRCD": true,
//              "isCritical": false
//            }
//          ],
//          "tPower": 4500
//        }
//      ]
//    },
//    {
//        "floorName": "Second Floor",
//      "rooms": [
//        {
//            "name": "Office",
//          "area": true,
//          "rating": 4,
//          "Consumer": [
//            {
//                "id": 11,
//              "name": "Computer",
//              "watt": 400,
//              "contactor": false,
//              "separateRCD": false,
//              "isCritical": true
//            },
//            {
//                "id": 12,
//              "name": "Printer",
//              "watt": 200,
//              "contactor": false,
//              "separateRCD": false,
//              "isCritical": false
//            },
//            {
//                "id": 13,
//              "name": "Lighting",
//              "watt": 300,
//              "contactor": false,
//              "separateRCD": false,
//              "isCritical": false
//            }
//          ],
//          "tPower": 900
//        }
//      ]
//    }
//  ]
//}

