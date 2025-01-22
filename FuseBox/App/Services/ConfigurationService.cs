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

            // Расчет параметров устройства электрощита
            Shield shield;

            if (input.InitialSettings.PhasesCount == 1) // Колличество фаз
            {
                shield = ConfigureShield(input);
            }
            else
            {
                shield = ConfigureShield3(input);
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
        private Shield ConfigureShield(Project project)
        {
            int totalPower = project.CalculateTotalPower();

            // Создаем список всех потребителей и записываем их
            List<Consumer> AllConsumers = new List<Consumer>();

            foreach (var floor in project.Floors)
            {
                foreach (var room in floor.Rooms)
                {
                    foreach (var equipment in room.Equipments)
                    {
                        AllConsumers.Add(equipment);
                    }
                }
            }

            // Создаем список всех АВ автоматов для оборудовния
            List<Module> AVFuses = new List<Module>();

            // Создаем список входной группы устройств
            List<Module> InputGroupOfModules = new List<Module>();


            if (project.Shield.MainBreaker)
            {
                InputGroupOfModules.Add(new Module("Introductory", project.InitialSettings.MainAmperage, 2, false, 35));
            }

            if (project.Shield.SurgeProtection)
            {
                InputGroupOfModules.Add(new Module("SPD", 100, 2, false, 65));
            }

            if (project.Shield.LoadSwitch2P)
            {
                InputGroupOfModules.Add(new Module("LoadSwitch", 63, 2, false, 35));
            }

            if (project.Shield.RailMeter)
            {
                InputGroupOfModules.Add(new Module("DinRailMeter", 63, 6, false, 145));
            }

            if (project.Shield.FireUZO)
            {
                InputGroupOfModules.Add(new Module("RCDFire", 63, 2, false, 75)); // УЗО часто критическое
            }

            //if (project.Shield.ModularContactor)
            //{
            //    InputGroupOfModules.Add(new Module("ModularContactor", 25, 1, false, 84));
            //}

            if (project.Shield.VoltageRelay)
            {
                InputGroupOfModules.Add(new Module("VoltageRelay", 16, 2, false, 40));
            }

            if (project.Shield.RailSocket)
            {
                InputGroupOfModules.Add(new Module("DinRailSocket", 16, 3, false, 22));
            }

            if (project.Shield.NDisconnectableLine)
            {
                InputGroupOfModules.Add(new Module("NonDisconnectableLine", 25, 2, false, 43)); // Обязательно критическая линия
            }

            if (project.Shield.LoadSwitch)
            {
                InputGroupOfModules.Add(new Module("LoadSwitch", 63, 2, false, 35));
            }

            if (project.Shield.CrossModule)
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

            decimal totalAmper = totalPower / project.InitialSettings.VoltageStandard; // A


            // Автоматы с учетом сортировки: Свет, Розетки, Кондиционеры
            if (AllConsumers.Any(e => e.Name.Equals("lighting", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Lighting; i++)
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Sockets; i++)
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("air conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("heated floor", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }

            // Добавляем втоматы без сортировки
            foreach (var Equipment in AllConsumers)
            {
                if (Equipment.Name != "lighting" && Equipment.Name != "socket" && Equipment.Name != "air conditioner")
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }

            int avIndex = 0;
            int AVCount = AVFuses.Count - 1; // !!! -1 Это Костыль

            // Логика распределения УЗО от нагрузки
            if (totalAmper <= 8)
            {
                // Создаем УЗО
                InputGroupOfModules.Add(new Module("RCD", 16, 2, false, 43));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    InputGroupOfModules.Add(fuse);
                }
            }
            else if (totalAmper > 8 && totalAmper <= 16)
            {
                InputGroupOfModules.Add(new Module("RCD", 32, 2, false, 43));

                foreach (var fuse in AVFuses)
                {
                    InputGroupOfModules.Add(fuse);
                }
            }
            else if (totalAmper > 16 && totalAmper <= 32)
            {
                InputGroupOfModules.Add(new Module("RCD", 63, 2, false, 43));

                foreach (var fuse in AVFuses)
                {
                    InputGroupOfModules.Add(fuse);
                }
            }
            else
            {
                double countOfRCD = Math.Ceiling(totalPower / project.InitialSettings.VoltageStandard / 30.00);

                int countABPerRCD = Convert.ToInt32(Math.Ceiling(AVCount / countOfRCD));

                for (int i = 0; i < countOfRCD; i++)
                {
                    InputGroupOfModules.Add(new Module("RCD", 63, 2, false, 43));

                    // Добавляем созданые ранее АВ автоматы в список
                    for (int ii = 0; ii < countABPerRCD && avIndex < AVCount; ii++) // !!!
                    {
                        InputGroupOfModules.Add(AVFuses[avIndex]);
                        avIndex++;
                    }
                }
            }

            // Делаем единый список модулей в Щите
            List<Module> shieldModuleSet = new List<Module>(InputGroupOfModules);


            // Компоновка Щита по уровням...
            ShieldByLevel(project, shieldModuleSet);

            return project.Shield;
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        private Shield ConfigureShield3(Project project)
        {
            int totalPower = project.CalculateTotalPower();

            // Создаем список всех потребителей и записываем их
            List<Consumer> AllConsumers = new List<Consumer>();

            foreach (var floor in project.Floors)
            {
                foreach (var room in floor.Rooms)
                {
                    foreach (var equipment in room.Equipments)
                    {
                        AllConsumers.Add(equipment);
                    }
                }
            }

            // Создаем список всех АВ автоматов для оборудовния
            List<Module> AVFuses = new List<Module>();

            // Создаем список входной группы устройств
            List<Module> InputGroupOfModules = new List<Module>();

            // Настройки опцыонных автоматов \\

            if (project.Shield.MainBreaker)
            {
                // Если да, то добавляем 3 фазы + ноль
                if (project.Shield.Main3PN)
                {
                    InputGroupOfModules.Add(new Module("Introductory 3P+N", project.InitialSettings.MainAmperage, 3, false, 40));
                }
                else
                {
                    InputGroupOfModules.Add(new Module("Introductory 3P", project.InitialSettings.MainAmperage, 4, false, 35));
                }

            }

            if (project.Shield.SurgeProtection)
            {
                InputGroupOfModules.Add(new Module("SPD", 100, 2, false, 65));
            }

            //// Проверяем наличие выключателя 2P
            //if (project.Shield.LoadSwitch2P)
            //{
            //    project.Shield.Fuses.Add(new Module("LoadSwitch", 63, 2, false, 35));
            //}

            if (project.Shield.RailMeter)
            {
                InputGroupOfModules.Add(new Module("DinRailMeter", 63, 6, false, 145));
            }

            if (project.Shield.FireUZO)
            {
                InputGroupOfModules.Add(new Module("RCDFire", 63, 2, false, 75)); // УЗО часто критическое
            }

            if (project.Shield.ModularContactor)
            {
                InputGroupOfModules.Add(new Module("AV", 16, 1, false, 10));
                InputGroupOfModules.Add(new Module("ModularContactor", 25, 2, false, 60));
            }

            // Проверяем наличие реле напряжения
            if (project.Shield.VoltageRelay)
            {
                if (project.Shield.ThreePRelay)
                {
                    InputGroupOfModules.Add(new Module("Three-Phase Relay", 16, 6, false, 65));
                }
                else
                {
                    InputGroupOfModules.Add(new Module("Voltage Relay", 16, 2, false, 40));
                    InputGroupOfModules.Add(new Module("Voltage Relay", 16, 2, false, 40));
                    InputGroupOfModules.Add(new Module("Voltage Relay", 16, 2, false, 40));
                }

            }

            // Проверяем наличие розетки на DIN-рейку
            if (project.Shield.RailSocket)
            {
                InputGroupOfModules.Add(new Module("DinRailSocket", 16, 3, false, 22));
            }

            //// Проверяем наличие УЗО неотключаемой линии
            //if (project.Shield.NDisconnectableLine)
            //{
            //    project.Shield.Fuses.Add(new Module("NonDisconnectableLine", 25, 2, false, 43)); // Обязательно критическая линия
            //}

            // Проверяем наличие общего выключателя
            if (project.Shield.LoadSwitch)
            {
                InputGroupOfModules.Add(new Module("LoadSwitch", 63, 2, false, 35));
            }

            // Проверяем наличие кросс-модуля
            if (project.Shield.CrossModule)
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

            decimal totalAmper = totalPower / project.InitialSettings.VoltageStandard; // A


            // Автоматы с учетом сортировки: Свет, Розетки, Кондиционеры
            if (AllConsumers.Any(e => e.Name.Equals("lighting", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Lighting; i++)
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Sockets; i++)
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("air conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("heated floor", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }

            // Добавляем втоматы без сортировки
            foreach (var Equipment in AllConsumers)
            {
                if (Equipment.Name != "lighting" && Equipment.Name != "socket" && Equipment.Name != "air conditioner")
                {
                    AVFuses.Add(new Module("AV", 16, 1, false, 10));
                }
            }

            int avIndex = 0;
            int AVCount = AVFuses.Count - 1; // !!! -1 Это Костыль

            // Логика распределения УЗО от нагрузки
            if (totalAmper <= 8)
            {
                // Создаем УЗО
                InputGroupOfModules.Add(new Module("RCD", 16, 2, false, 43));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    InputGroupOfModules.Add(fuse);
                }
            }
            else if (totalAmper > 8 && totalAmper <= 16)
            {
                InputGroupOfModules.Add(new Module("RCD", 32, 2, false, 43));

                foreach (var fuse in AVFuses)
                {
                    InputGroupOfModules.Add(fuse);
                }
            }
            else if (totalAmper > 16 && totalAmper <= 32)
            {
                InputGroupOfModules.Add(new Module("RCD", 63, 2, false, 43));

                foreach (var fuse in AVFuses)
                {
                    InputGroupOfModules.Add(fuse);
                }
            }
            else
            {
                double countOfRCD = Math.Ceiling(totalPower / project.InitialSettings.VoltageStandard / 30.00);

                int countABPerRCD = Convert.ToInt32(Math.Ceiling(AVCount / countOfRCD));

                for (int i = 0; i < countOfRCD; i++)
                {
                    InputGroupOfModules.Add(new Module("RCD", 63, 2, false, 43));

                    // Добавляем созданые ранее АВ автоматы в список
                    for (int ii = 0; ii < countABPerRCD && avIndex < AVCount; ii++) // !!!
                    {
                        InputGroupOfModules.Add(AVFuses[avIndex]);
                        avIndex++;
                    }
                }
            }


            // Компоновка Щита по уровням...


            // Делаем единый список модулей в Щите
            List<Module> shieldModuleSet = new List<Module>(InputGroupOfModules);
            // shieldModuleSet.AddRange(Fuses);

            ShieldByLevel(project, shieldModuleSet);

            return project.Shield;
        }

        public List<Module> ShieldByLevel(Project project, List<Module>  shieldModuleSet) // Логика распределения модулей по уровням
        {

            // Логика распределения модулей по уровням

            double countOfSlots = 0;

            // Вычисляем общее количество слотов для Щитовой панели
            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                countOfSlots += shieldModuleSet[i].Slots;
            }

            var countOfDINLevels = Math.Ceiling(countOfSlots / project.InitialSettings.ShieldWidth); //Количество уровней ДИН рейки в Щите

            // Инициализируем списки каждого уровня щита
            for (int i = 0; i < countOfDINLevels; i++)
            {
                project.Shield.Fuses.Add(new List<Module>());
            }

            project.Shield.DINLines = (int)countOfDINLevels; // Запись в поле объекта количество уровней в щите (Как по мне лишнее)

            int startPos = 0;
            int endPos = 0;
            int occupiedSlots = 0;
            int currentLevel = 0;
            int emptySlotsOnDINLevel = 0;
            int shieldWidth = project.InitialSettings.ShieldWidth;

            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                occupiedSlots += (int)shieldModuleSet[i].Slots;

                if (occupiedSlots >= shieldWidth)
                {
                    if ((occupiedSlots > shieldWidth) && (occupiedSlots != shieldWidth))
                    {
                        emptySlotsOnDINLevel = shieldWidth - (occupiedSlots - (int)shieldModuleSet[i].Slots);
                        shieldModuleSet.Insert(i, new Module("{empty space}", 0, emptySlotsOnDINLevel, false, 0)); // i-ый элемент становится i+1, а пустой - i-ым
                        endPos = i + 1;
                        project.Shield.Fuses[currentLevel].AddRange(shieldModuleSet.GetRange(startPos, endPos - startPos));
                        startPos = endPos;
                        occupiedSlots = 0;
                        currentLevel++;
                        continue;
                    }
                    if (occupiedSlots == shieldWidth)
                    {
                        endPos = i + 1;
                        project.Shield.Fuses[currentLevel].AddRange(shieldModuleSet.GetRange(startPos, endPos - startPos));
                        startPos = endPos;
                        occupiedSlots = 0;
                        currentLevel++;
                    }

                }
                if (i == shieldModuleSet.Count - 1 && occupiedSlots != shieldWidth)
                {
                    endPos = i + 1;
                    project.Shield.Fuses[currentLevel].AddRange(shieldModuleSet.GetRange(startPos, endPos - startPos));
                    project.Shield.Fuses[currentLevel].Add(new Module("{empty space}", 0, shieldWidth - occupiedSlots, false, 0));
                    currentLevel++;

                }
                if (currentLevel > countOfDINLevels) break;
                
            }
            return shieldModuleSet;
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
}


//{
//    "floorGrouping": {
//        "FloorGroupingP": true,
//    "separateUZO": true
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
//          "equipments": [
//            {
//                "id": 1,
//              "name": "TV",
//              "watt": 150,
//              "contactor": false,
//              "separateRCD": false,
//              "isCritical": false
//            },
//            {
//                "id": 2,
//              "name": "Air Conditioner",
//              "watt": 2000,
//              "contactor": true,
//              "separateRCD": true,
//              "isCritical": true
//            },
//            {
//                "id": 3,
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
//          "equipments": [
//            {
//                "id": 4,
//              "name": "Refrigerator",
//              "watt": 800,
//              "contactor": false,
//              "separateRCD": true,
//              "isCritical": true
//            },
//            {
//                "id": 5,
//              "name": "Microwave",
//              "watt": 1200,
//              "contactor": false,
//              "separateRCD": true,
//              "isCritical": false
//            },
//            {
//                "id": 6,
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
//          "equipments": [
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
//          "equipments": [
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
//          "equipments": [
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
