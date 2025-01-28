using FuseBox.App.Models;
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
            List<BaseElectrical> AllConsumers = CalculateAllConsumers(input);

            // Расчет параметров устройства электрощита
            FuseBox shield;

            if (input.InitialSettings.PhasesCount == 1) // Колличество фаз
            {
                shield = ConfigureShield(input, AllConsumers);
            }
            else
            {
                shield = ConfigureShield(input, AllConsumers);
                //shield = ConfigureShield3(input, AllConsumers, totalAmper);
            }

            // Возвращаем новый модифицированный объект
            return new Project
            {
                InitialSettings = input.InitialSettings,
                GlobalGrouping = input.GlobalGrouping,
                FuseBox = shield, // Возвращаем настроенный щит, а все остальное так же
                FloorGrouping = input.FloorGrouping,
                Floors = input.Floors,
            };
        }

        // Логика конфигурации устройств...
        private FuseBox ConfigureShield(Project project, List<BaseElectrical> AllConsumers)
        {
            // Создаем список всех АВ автоматов для оборудовния
            List<Fuse> AVFuses = new List<Fuse>();

            // Делаем единый список модулей в Щите
            List<Component> shieldModuleSet = new List<Component>();

            if (project.FuseBox.MainBreaker)
            {
                shieldModuleSet.Add(new Introductory("Introductory", project.InitialSettings.MainAmperage, 2, 2, 35, "P1"));
            }

            if (project.FuseBox.SurgeProtection)
            {
                shieldModuleSet.Add(new Component("SPD", 100, 2, 2, 65));
            }

            if (project.FuseBox.LoadSwitch2P)
            {
                shieldModuleSet.Add(new Component("LoadSwitch", 63, 2, 2, 35));
            }

            if (project.FuseBox.RailMeter)
            {
                shieldModuleSet.Add(new Component("DinRailMeter", 63, 6, 2, 145));
            }

            if (project.FuseBox.FireUZO)
            {
                shieldModuleSet.Add(new RCDFire(300, "RCDFire", 63, 2, 2, 75)); // УЗО часто критическое
            }

            if (project.FuseBox.VoltageRelay)
            {
                shieldModuleSet.Add(new Component("VoltageRelay", 16, 2, 2, 40));
            }

            if (project.FuseBox.RailSocket)
            {
                shieldModuleSet.Add(new Component("DinRailSocket", 16, 3, 2, 22));
            }

            if (project.FuseBox.NDisconnectableLine)
            {
                shieldModuleSet.Add(new RCD("NonDisconnectableLine", 25, 2, 2, 43, 30)); // Критическая линия
            }

            if (project.FuseBox.LoadSwitch)
            {
                shieldModuleSet.Add(new Component("LoadSwitch", 63, 2, 2, 35));
            }

            if (project.FuseBox.ModularContactor)
            {
                shieldModuleSet.Add(new Contactor("ModularContactor", 100, 4, 2, 25, project.FuseBox.Contactor));
            }

            if (project.FuseBox.CrossModule)
            {
                shieldModuleSet.Add(new Component("CrossBlock", 100, 4, 2, 25)); // Кросс-блок может быть без номинала
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
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Sockets; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("air conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("heated floor", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10));
                }
            }

            foreach (var consumer in AllConsumers) // Добавляем втоматы без сортировки
            {

                if (consumer.Name != "lighting" && consumer.Name != "socket" && consumer.Name != "air conditioner")
                {
                    //List<Consumer> Consumers = new List<Consumer>();
                    //Consumers.Add(consumer);
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10)); // Consumers
                }
            }

            int avIndex = 0;
            int TAmper = project.CalculateTotalPower();
            // int AVCount = AVFuses.Count - 1; // !!! -1 Это Костыль

            // Логика распределения УЗО от нагрузки
            if (TAmper <= 8)
            {
                // Создаем УЗО
                shieldModuleSet.Add(new RCD("RCD", 16, 2, 2, 43, 2));

                // Добавляем созданые ранее АВ автоматы в список
                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else if (TAmper > 8 && TAmper <= 16)
            {
                shieldModuleSet.Add(new RCD("RCD", 32, 2, 2, 43, 2));

                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else if (TAmper > 16 && TAmper <= 32)
            {
                shieldModuleSet.Add(new RCD("RCD", 63, 2, 2, 43, 2));

                foreach (var fuse in AVFuses)
                {
                    shieldModuleSet.Add(fuse);
                }
            }
            else
            {
                double countOfRCD = Math.Ceiling(project.CalculateTotalPower() / 32.00);

                int countABPerRCD = Convert.ToInt32(Math.Ceiling(AVFuses.Count / countOfRCD));

                for (int i = 0; i < countOfRCD; i++)
                {
                    shieldModuleSet.Add(new RCD("RCD", 63, 2, 2, 43, 2));

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

            return project.FuseBox;
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        //private FuseBox ConfigureShield3(Project project, List<BaseElectrical> AllConsumers, decimal totalAmper)
        //{

        //    // Создаем список всех АВ автоматов для оборудовния
        //    List<Fuse> AVFuses = new List<Fuse>();

        //    // Делаем единый список модулей в Щите
        //    List<Component> shieldModuleSet = new List<Component>();

        //    // Настройки опцыонных автоматов \\
        //    if (project.FuseBoxPanel.MainBreaker)
        //    {
        //        // Если да, то добавляем 3 фазы + ноль
        //        if (project.FuseBoxPanel.Main3PN)
        //        {
        //            shieldModuleSet.Add(new Introductory("Introductory 3P+N", project.InitialSettings.MainAmperage, 2, false, 35, false, false));
        //        }
        //        else
        //        {
        //            shieldModuleSet.Add(new Introductory("Introductory 3P", project.InitialSettings.MainAmperage, 2, false, 35, false, false));
        //        }
        //    }

        //    if (project.FuseBoxPanel.SurgeProtection)
        //    {
        //        shieldModuleSet.Add(new Module("SPD", 100, 2, false, 65, false, "class 2"));
        //    }

        //    if (project.FuseBoxPanel.RailMeter)
        //    {
        //        shieldModuleSet.Add(new Module("DinRailMeter", 63, 6, false, 145, false, ""));
        //    }

        //    if (project.FuseBoxPanel.FireUZO)
        //    {
        //        shieldModuleSet.Add(new RCDFire("RCDFire", 63, 2, false, 75, 300, false)); // УЗО часто критическое
        //    }

        //    if (project.FuseBoxPanel.ModularContactor)
        //    {
        //        shieldModuleSet.Add(new Contactor());
        //        shieldModuleSet.Add(new Contactor());
        //    }

        //    // Проверяем наличие реле напряжения
        //    if (project.FuseBoxPanel.VoltageRelay)
        //    {
        //        if (project.FuseBoxPanel.ThreePRelay)
        //        {
        //            shieldModuleSet.Add(new Module("Three-Phase Relay", 16, 6, false, 60, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));

        //        }
        //        else
        //        {
        //            shieldModuleSet.Add(new Module("VoltageRelay", 16, 2, false, 40, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));
        //            shieldModuleSet.Add(new Module("VoltageRelay", 16, 2, false, 40, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));
        //            shieldModuleSet.Add(new Module("VoltageRelay", 16, 2, false, 40, false, Convert.ToString(project.InitialSettings.VoltageStandard) + "v"));
        //        }
        //    }

        //    // Проверяем наличие розетки на DIN-рейку
        //    if (project.FuseBoxPanel.RailSocket)
        //    {
        //        shieldModuleSet.Add(new Module("DinRailSocket", 16, 3, false, 22, false, "UK"));
        //    }

        //    // Проверяем наличие общего выключателя
        //    if (project.FuseBoxPanel.LoadSwitch)
        //    {
        //        shieldModuleSet.Add(new Module("LoadSwitch", 63, 2, false, 35, false, "class 2"));
        //    }

        //    // Проверяем наличие кросс-модуля
        //    if (project.FuseBoxPanel.CrossModule)
        //    {
        //        shieldModuleSet.Add(new Module("CrossBlock", 100, 4, false, 25, false, "class 2")); // Кросс-блок может быть без номинала
        //    }

        //    // Настройки автоматов для техники \\
        //    // Автоматы с учетом сортировки: Свет, Розетки, Кондиционеры
        //    if (AllConsumers.Any(e => e.Name.Equals("lighting", StringComparison.OrdinalIgnoreCase)))
        //    {
        //        for (int i = 0; i < project.GlobalGrouping.Lighting; i++)
        //        {
        //            AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
        //        }
        //    }
        //    if (AllConsumers.Any(e => e.Name.Equals("socket", StringComparison.OrdinalIgnoreCase)))
        //    {
        //        for (int i = 0; i < project.GlobalGrouping.Sockets; i++)
        //        {
        //            AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
        //        }
        //    }
        //    if (AllConsumers.Any(e => e.Name.Equals("air conditioner", StringComparison.OrdinalIgnoreCase)))
        //    {
        //        for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
        //        {
        //            AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
        //        }
        //    }
        //    if (AllConsumers.Any(e => e.Name.Equals("heated floor", StringComparison.OrdinalIgnoreCase)))
        //    {
        //        for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
        //        {
        //            AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
        //        }
        //    }

        //    // Добавляем втоматы без сортировки
        //    foreach (var Equipment in AllConsumers)
        //    {
        //        if (Equipment.Name != "lighting" && Equipment.Name != "socket" && Equipment.Name != "air conditioner")
        //        {
        //            AVFuses.Add(new Fuse("AV", 16, 1, false, 10, false));
        //        }
        //    }

        //    int avIndex = 0;
        //    //int AVCount = AVFuses.Count - 1; // !!! -1 Это Костыль

        //    // Логика распределения УЗО от нагрузки
        //    if (totalAmper <= 8)
        //    {
        //        // Создаем УЗО
        //        shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

        //        // Добавляем созданые ранее АВ автоматы в список
        //        foreach (var fuse in AVFuses)
        //        {
        //            shieldModuleSet.Add(fuse);
        //        }
        //    }
        //    else if (totalAmper > 8 && totalAmper <= 16)
        //    {
        //        shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

        //        foreach (var fuse in AVFuses)
        //        {
        //            shieldModuleSet.Add(fuse);
        //        }
        //    }
        //    else if (totalAmper > 16 && totalAmper <= 32)
        //    {
        //        shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

        //        foreach (var fuse in AVFuses)
        //        {
        //            shieldModuleSet.Add(fuse);
        //        }
        //    }
        //    else
        //    {
        //        double countOfRCD = Math.Ceiling(project.CalculateTotalPower() / project.InitialSettings.VoltageStandard / 30.00);

        //        int countABPerRCD = Convert.ToInt32(Math.Ceiling(AVFuses.Count / countOfRCD));

        //        for (int i = 0; i < countOfRCD; i++)
        //        {
        //            shieldModuleSet.Add(new RCD("RCD", 16, 2, false, 43, 2, false));

        //            // Добавляем созданые ранее АВ автоматы в список
        //            for (int ii = 0; ii < countABPerRCD && avIndex < AVFuses.Count - 1; ii++) // !!!
        //            {
        //                shieldModuleSet.Add(AVFuses[avIndex]);
        //                avIndex++;
        //            }
        //        }
        //    }

        //    // Компоновка Щита по уровням...
        //    ShieldByLevel(project, shieldModuleSet);

        //    return project.FuseBoxPanel;
        //}

        // Логика распределения модулей по уровням...
        public void ShieldByLevel(Project project, List<Component> shieldModuleSet)
        {
            double countOfSlots = shieldModuleSet.Sum(e => e.Slots); // Вычисляем общее количество слотов для Щитовой панели

            //for (int i = 0; i < shieldModuleSet.Count; i++)
            //    countOfSlots += shieldModuleSet[i].Slots;

            var countOfDINLevels = Math.Ceiling(countOfSlots / project.InitialSettings.ShieldWidth); //Количество уровней ДИН рейки в Щите

            // Инициализируем списки каждого уровня щита по ПЕРВИЧНЫМ ДАННЫМ (без учёта потенциальных пустых мест)
            for (int i = 0; i < countOfDINLevels; i++)
                project.FuseBox.Components.Add(new List<BaseElectrical>());

            project.FuseBox.DINLines = (int)countOfDINLevels; // Запись в поле объекта количество уровней в щите (Как по мне лишнее)
            int occupiedSlots = 0;
            int currentLevel = 0;
            int shieldWidth = project.InitialSettings.ShieldWidth;

            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                if (currentLevel >= project.FuseBox.Components.Count)
                    project.FuseBox.Components.Add(new List<BaseElectrical>()); // Добавляем новый уровень, если его ещё нет

                if (shieldModuleSet[i].Name == "RCD") // i-й элемент - УЗО, значит дальше автоматы, с ними связанные                
                    IsRCDBlockFitAtLevel(project, shieldModuleSet, ref i, ref occupiedSlots, ref currentLevel, shieldWidth);

                else // i-й элемент - другой модуль, значит применяется обычная логика                
                    IsModuleFitAtLevel(project, shieldModuleSet, ref i, ref occupiedSlots, ref currentLevel, shieldWidth);

                if (occupiedSlots < shieldWidth && i == shieldModuleSet.Count - 1)
                    project.FuseBox.Components[currentLevel].Add(new Component("{empty space}", 0, shieldWidth - occupiedSlots, 0, 0));
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
                    project.FuseBox.Components[currentLevel].Add(new Component("{empty space}", 0, shieldWidth - occupiedSlots, 0, 0)); // Добавлена проверка на добавление доп. уровня ниже

                occupiedSlots = 0;
                currentLevel++;
                if (currentLevel >= project.FuseBox.Components.Count)
                    project.FuseBox.Components.Add(new List<BaseElectrical>()); // Добавляем новый уровень, если его ещё нет                

                for (int k = i; k < j; k++)
                    project.FuseBox.Components[currentLevel].Add(shieldModuleSet[k]);

                occupiedSlots += (int)rcdBlockSlots;
            }
            else
            {
                occupiedSlots += (int)rcdBlockSlots;
                for (int k = i; k < j; k++)
                    project.FuseBox.Components[currentLevel].Add(shieldModuleSet[k]);
            }
            i = j - 1; // Пропускаем обработанные AV
        }
        public static void IsModuleFitAtLevel(Project project, List<Component> shieldModuleSet, ref int i, ref int occupiedSlots, ref int currentLevel, int shieldWidth)
        {
            occupiedSlots += (int)shieldModuleSet[i].Slots;
            if (occupiedSlots < shieldWidth)                // место есть как для модуля, так и после него на уровне
            {
                project.FuseBox.Components[currentLevel].Add(shieldModuleSet[i]);
            }
            else if (occupiedSlots > shieldWidth)           // модуль не помещается на уровне. Проверку и добавление уровней делать не надо!
            {
                project.FuseBox.Components[currentLevel].Add(new Component("{empty space}", 0, shieldWidth - (occupiedSlots - (int)shieldModuleSet[i].Slots), 0, 0));
                currentLevel++;
                occupiedSlots = (int)shieldModuleSet[i].Slots;
                project.FuseBox.Components[currentLevel].Add(shieldModuleSet[i]);
            }
            else // Слотов на уровне аккурат равно длине шины
            {
                project.FuseBox.Components[currentLevel].Add(shieldModuleSet[i]);
                currentLevel++;
                occupiedSlots = 0;
            }
        }

        public List<BaseElectrical> CalculateAllConsumers(Project project)
        {
            List<BaseElectrical> AllConsumers = new List<BaseElectrical>();
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


/*

{
    "floorGrouping": {
        "FloorGroupingP": true,
        "separateUZO": true
    },
  "globalGrouping": {
    "Sockets": 1,
    "Lighting": 1,
    "Conditioners": 1
  },
  "initialSettings": {
    "PhasesCount": 1,
    "MainAmperage": 25,
    "ShieldWidth": 16,
    "VoltageStandard": 220,
    "PowerCoefficient": 1
  },
  "FuseBox": {
    "MainBreaker": true,
    "Main3PN": false,
    "SurgeProtection": true,
    "LoadSwitch2P": true,
    "ModularContactor": true,
    "RailMeter": true,
    "FireUZO": true,
    "VoltageRelay": true,
    "RailSocket": true,
    "NDisconnectableLine": true,
    "LoadSwitch": true,
    "CrossModule": true,
    "DINLines": 1,
    "Price": 1000
  },
    "floors": [
      {
      "Name": "Ground Floor",
      "rooms": [
        {
          "Name": "Living Room",
          "Consumer": [
            {
              "Id": 1,
              "name": "TV",
              "Amper": 1
            },
            {
              "Id": 2,
              "name": "Air Conditioner",
              "Amper": 8
            },
            {
              "Id": 3,
              "name": "Lighting",
              "Amper": 1
            }
          ],
          "tPower": 10
        },
        {
          "name": "Kitchen",
          "Consumer": [
            {
              "Id": 4,
              "name": "Refrigerator",
              "Amper": 3
            },
            {
              "Id": 5,
              "name": "Microwave",
              "Amper": 5
            },
            {
              "Id": 6,
              "name": "Oven",
              "Amper": 7
            }
          ],
          "tPower": 15
        }
      ]
    },
    {
      "Name": "First Floor",
      "rooms": [
        {
          "name": "Bedroom 1",
          "Consumer": [
            {
              "id": 7,
              "name": "Heater",
              "Amper": 4
            },
            {
              "id": 8,
              "name": "Fan",
              "Amper": 1
            }
          ],
          "tPower": 5
        },
        {
          "name": "Bathroom",
          "Consumer": [
            {
              "id": 9,
              "name": "Water Heater",
              "Amper": 13
            },
            {
              "id": 10,
              "name": "Hair Dryer",
              "Amper": 7
            }
          ],
          "tPower": 20
        }
      ]
    },
    {
      "Name": "Second Floor",
      "rooms": [
        {
          "name": "Office",
          "Consumer": [
            {
              "id": 11,
              "name": "Computer",
              "Amper": 2
            },
            {
              "id": 12,
              "name": "Printer",
              "Amper": 1
            },
            {
              "id": 13,
              "name": "Lighting",
              "Amper": 2
            }
          ],
          "totalPower": 40
        }
      ]
    }
  ]
}

*/