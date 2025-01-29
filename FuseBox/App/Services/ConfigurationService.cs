using FuseBox.App.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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
                shield = ConfigureShield3(input, AllConsumers);
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

            List<Fuse> AVFuses = new List<Fuse>();
            List<Component> shieldModuleSet = new List<Component>();
            List<RCD> uzos = new List<RCD>();

            if (project.FuseBox.MainBreaker)        {shieldModuleSet.Add(new Introductory("Introductory", project.InitialSettings.MainAmperage, 2, 2, 35, "P1"));}
            if (project.FuseBox.SurgeProtection)    {shieldModuleSet.Add(new Component   ("SPD", 100, 2, 2, 65));}
            if (project.FuseBox.LoadSwitch2P)       {shieldModuleSet.Add(new Component   ("LoadSwitch", 63, 2, 2, 35));}
            if (project.FuseBox.RailMeter)          {shieldModuleSet.Add(new Component   ("DinRailMeter", 63, 6, 2, 145));}
            if (project.FuseBox.FireUZO)            {shieldModuleSet.Add(new RCDFire     ("RCDFire", 63, 2, 2, 75, 300));}
            if (project.FuseBox.VoltageRelay)       {shieldModuleSet.Add(new Component   ("VoltageRelay", 16, 2, 2, 40));}
            if (project.FuseBox.RailSocket)         {shieldModuleSet.Add(new Component   ("DinRailSocket", 16, 3, 2, 22));}
            if (project.FuseBox.NDisconnectableLine){shieldModuleSet.Add(new RCD         ("NonDisconnectableLine", 25, 2, 2, 43, 30, new List<BaseElectrical>()));}
            if (project.FuseBox.LoadSwitch)         {shieldModuleSet.Add(new Component   ("LoadSwitch", 63, 2, 2, 35));}
            if (project.FuseBox.ModularContactor)   {shieldModuleSet.Add(new Contactor   ("ModularContactor", 100, 4, 2, 25, project.FuseBox.Contactor));}
            if (project.FuseBox.CrossModule)        {shieldModuleSet.Add(new Component   ("CrossBlock", 100, 4, 2, 25));}

            // Настройки автоматов для техники \\
            // Примерная мощность автомата С16 - 3.6 кВт.

            // Примерная мощность УЗО      10А - 2.2 кВт.
            // Примерная мощность УЗО      32А - 7 кВт.
            // Примерная мощность УЗО      63А - 13,9 кВт. 

            // Типы УЗО на 10-300 мА ()	10 А	16 А	25 А	32 А	40 А	64 А	80 А	100 А
            // <30 мА – для защиты человека, 100> мА – для защиты зданий от пожаров

            // 118 A

            // Логика распределения потребителей
            DistributeOfConsumers(project, AllConsumers, AVFuses);

            // Логика распределения УЗО от нагрузки
            DistributeRCDFromLoad(project, uzos, AVFuses);

            shieldModuleSet.AddRange(uzos);

            // Компоновка Щита по уровням...
            ShieldByLevel(project, shieldModuleSet);

            return project.FuseBox;
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        private FuseBox ConfigureShield3(Project project, List<BaseElectrical> AllConsumers)
        {
            List<Fuse> AVFuses = new List<Fuse>();
            List<Component> shieldModuleSet = new List<Component>();
            List<RCD> uzos = new List<RCD>();


            // Настройки опцыонных автоматов \\

            if (project.FuseBox.MainBreaker)
            {
                // Если да, то добавляем 3 фазы + ноль
                if (project.FuseBox.Main3PN)
                {
                    shieldModuleSet.Add(new Introductory("Introductory 3P+N", project.InitialSettings.MainAmperage, 2, 2, 35, "P1"));
                }
                else
                {
                    shieldModuleSet.Add(new Introductory("Introductory 3P", project.InitialSettings.MainAmperage, 2, 2, 35, "P1"));
                }
            }
            if (project.FuseBox.SurgeProtection) { shieldModuleSet.Add(new Component("SPD", 100, 2, 2, 65)); }
            if (project.FuseBox.RailMeter)       { shieldModuleSet.Add(new Component("DinRailMeter", 63, 6, 2, 145)); }
            if (project.FuseBox.FireUZO)         { shieldModuleSet.Add(new RCDFire  ("RCDFire", 63, 2, 2, 75, 300)); }
            if (project.FuseBox.VoltageRelay)
            {
                if (project.FuseBox.ThreePRelay)
                {
                    shieldModuleSet.Add(new Component("VoltageRelay", 16, 2, 2, 60));

                }
                else
                {
                    shieldModuleSet.Add(new Component("VoltageRelay", 16, 2, 2, 40));
                    shieldModuleSet.Add(new Component("VoltageRelay", 16, 2, 2, 40));
                    shieldModuleSet.Add(new Component("VoltageRelay", 16, 2, 2, 40));
                }
            }
            if (project.FuseBox.RailSocket)      { shieldModuleSet.Add(new Component("DinRailSocket", 16, 3, 2, 22)); }
            if (project.FuseBox.LoadSwitch)      { shieldModuleSet.Add(new Component("LoadSwitch", 63, 2, 2, 35)); }
            if (project.FuseBox.ModularContactor){ shieldModuleSet.Add(new Contactor("ModularContactor", 100, 4, 2, 25, project.FuseBox.Contactor)); }
            if (project.FuseBox.CrossModule)     { shieldModuleSet.Add(new Component("CrossBlock", 100, 4, 2, 25)); }




            // Логика распределения потребителей
            DistributeOfConsumers(project, AllConsumers, AVFuses);

            // Логика распределения УЗО от нагрузки
            DistributeRCDFromLoad(project, uzos, AVFuses);

            shieldModuleSet.AddRange(uzos);

            // Компоновка Щита по уровням...
            ShieldByLevel(project, shieldModuleSet);

            return project.FuseBox;
        }



        // Логика распределения модулей по порядку
        public void DistributeOfConsumers(Project project, List<BaseElectrical> AllConsumers, List<Fuse> AVFuses)
        {
            // Логика распределения потребителей

            List<BaseElectrical> Lighting = new();
            List<BaseElectrical> Socket = new();
            List<BaseElectrical> AirConditioner = new();
            List<BaseElectrical> HeatedFloor = new();

            var consumerGroups = new Dictionary<string, List<BaseElectrical>>
            {
                { "Lighting", Lighting },
                { "Socket", Socket },
                { "Air Conditioner", AirConditioner },
                { "Heated Floor", HeatedFloor }
            };

            foreach (var consumer in AllConsumers)
            {
                if (consumerGroups.ContainsKey(consumer.Name))
                {
                    consumerGroups[consumer.Name].Add(consumer);
                }
            }

            // Автоматы с учетом сортировки: Свет, Розетки, Кондиционеры
            if (AllConsumers.Any(e => e.Name.Equals("Lighting", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Lighting; i++)
                {
                    var L = DistributeEvenly(Lighting, project.GlobalGrouping.Lighting); // Выбирает только весь свет
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10, L[i]));                     // И пихает его в автомат
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("Socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Sockets; i++)
                {
                    var L = DistributeEvenly(Socket, project.GlobalGrouping.Sockets);
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10, L[i]));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("Air Conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < project.GlobalGrouping.Conditioners; i++)
                {
                    var L = DistributeEvenly(AirConditioner, project.GlobalGrouping.Conditioners);
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10, L[i]));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("Heated Floor", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < 1; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10, HeatedFloor));
                }
            }
            foreach (var consumer in AllConsumers) // Добавляем автоматы без сортировки
            {
                if (consumer.Name != "Lighting" && consumer.Name != "Socket" && consumer.Name != "Air Conditioner" && consumer.Name != "Heated Floor")
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 1, 10, new List<BaseElectrical> { consumer }));
                }
            }
        }
        public void DistributeRCDFromLoad(Project project, List<RCD> uzos, List<Fuse> AVFuses)
        {
            // Логика распределения УЗО от нагрузки

            int TAmper = project.CalculateTotalPower();

            if (TAmper <= 8)
            {
                // Создаем УЗО
                uzos.Add(new RCD("RCD", 16, 2, 2, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else if (TAmper > 8 && TAmper <= 16)
            {
                uzos.Add(new RCD("RCD", 32, 2, 2, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else if (TAmper > 16 && TAmper <= 32)
            {
                uzos.Add(new RCD("RCD", 63, 2, 2, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else
            {
                double countOfRCD = Math.Ceiling(project.CalculateTotalPower() / 32.00);

                for (int i = 0; i < countOfRCD; i++)
                {
                    uzos.Add(new RCD("RCD", 63, 2, 2, 43, 2, new List<BaseElectrical>()));
                }
                DistributeBreakersToRCDs(AVFuses, uzos);
            }
        }
        public void DistributeBreakersToRCDs(List<Fuse> breakers, List<RCD> uzos)
        {
            // Сортируем УЗО по их текущей нагрузке, чтобы равномерно распределять
            var uzoLoads = uzos.ToDictionary(uzo => uzo, uzo => 0); // Создаем словарь: УЗО -> текущая мощность (нагрузка)

            foreach (var breaker in breakers)
            {
                // Вычисляем мощность автомата как сумму всех его потребителей
                int breakerLoad = breaker.Electricals.Sum(consumer => consumer.Amper);

                // Находим УЗО с минимальной текущей нагрузкой
                var targetUzo = uzoLoads.OrderBy(uz => uz.Value).First().Key;

                // Добавляем автомат к выбранному УЗО
                targetUzo.Electricals.Add(breaker);

                // Увеличиваем нагрузку для этого УЗО
                uzoLoads[targetUzo] += breakerLoad;
            }
        }
        public List<List<T>> DistributeEvenly<T>(List<T> items, int numberOfBuckets)
        {
            // Создаём пустые списки
            var buckets = new List<List<T>>(numberOfBuckets);
            for (int i = 0; i < numberOfBuckets; i++)
            {
                buckets.Add(new List<T>());
            }

            // Распределяем элементы равномерно
            for (int i = 0; i < items.Count; i++)
            {
                buckets[i % numberOfBuckets].Add(items[i]);
            }

            return buckets;
        }

        // Логика распределения модулей по уровням...
        public void ShieldByLevel(Project project, List<Component> shieldModuleSet)
        {
            double countOfSlots = shieldModuleSet.Sum(e => e.Slots); // Вычисляем общее количество слотов для Щитовой панели
            var countOfDINLevels = Math.Ceiling(countOfSlots / project.InitialSettings.ShieldWidth); //Количество уровней ДИН рейки в Щите

            // Инициализируем списки каждого уровня щита по ПЕРВИЧНЫМ ДАННЫМ (без учёта потенциальных пустых мест)
            for (int i = 0; i < countOfDINLevels; i++) project.FuseBox.Components.Add(new List<BaseElectrical>());

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
            var rcd = shieldModuleSet[i] as RCD;
            int rcdBlockSlots = rcd.RCDBlockSlots();


            if (occupiedSlots + rcdBlockSlots > shieldWidth)
            {
                if (occupiedSlots < shieldWidth)
                    project.FuseBox.Components[currentLevel].Add(new Component("{empty space}", 0, shieldWidth - occupiedSlots, 0, 0)); // Добавлена проверка на добавление доп. уровня ниже

                occupiedSlots = 0;
                currentLevel++;
                if (currentLevel >= project.FuseBox.Components.Count)
                    project.FuseBox.Components.Add(new List<BaseElectrical>()); // Добавляем новый уровень, если его ещё нет                

                project.FuseBox.Components[currentLevel].Add(shieldModuleSet[i]); // Добавляем УЗО и все его автоматы в нем
                //for (int k = i; k < j; k++)
                //    project.FuseBox.Components[currentLevel].Add(shieldModuleSet[k]);

                occupiedSlots += (int)rcdBlockSlots;
            }
            else
            {
                occupiedSlots += (int)rcdBlockSlots;
                project.FuseBox.Components[currentLevel].Add(shieldModuleSet[i]);
            }

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
//        "PhasesCount": 1,
//    "MainAmperage": 25,
//    "ShieldWidth": 16,
//    "VoltageStandard": 220,
//    "PowerCoefficient": 1
//  },
//  "FuseBox": {
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
//    "DINLines": 1,
//    "Price": 1000
//  },
//    "floors": [
//      {
//        "Name": "Ground Floor",
//      "rooms": [
//        {
//            "Name": "Living Room",
//          "Consumer": [
//            {
//                "Id": 1,
//              "name": "TV",
//              "Amper": 1
//            },
//            {
//                "Id": 2,
//              "name": "Air Conditioner",
//              "Amper": 8
//            },
//            {
//                "Id": 3,
//              "name": "Lighting",
//              "Amper": 1
//            }
//          ],
//          "tPower": 10
//        },
//        {
//            "name": "Kitchen",
//          "Consumer": [
//            {
//                "Id": 4,
//              "name": "Refrigerator",
//              "Amper": 3
//            },
//            {
//                "Id": 5,
//              "name": "Microwave",
//              "Amper": 5
//            },
//            {
//                "Id": 6,
//              "name": "Oven",
//              "Amper": 7
//            }
//          ],
//          "tPower": 15
//        }
//      ]
//    },
//    {
//        "Name": "First Floor",
//      "rooms": [
//        {
//            "name": "Bedroom 1",
//          "Consumer": [
//            {
//                "id": 7,
//              "name": "Heater",
//              "Amper": 4
//            },
//            {
//                "id": 8,
//              "name": "Fan",
//              "Amper": 1
//            }
//          ],
//          "tPower": 5
//        },
//        {
//            "name": "Bathroom",
//          "Consumer": [
//            {
//                "id": 9,
//              "name": "Water Heater",
//              "Amper": 13
//            },
//            {
//                "id": 10,
//              "name": "Hair Dryer",
//              "Amper": 7
//            }
//          ],
//          "tPower": 20
//        }
//      ]
//    },
//    {
//        "Name": "Second Floor",
//      "rooms": [
//        {
//            "name": "Office",
//          "Consumer": [
//            {
//                "id": 11,
//              "name": "Computer",
//              "Amper": 2
//            },
//            {
//                "id": 12,
//              "name": "Printer",
//              "Amper": 1
//            },
//            {
//                "id": 13,
//              "name": "Lighting",
//              "Amper": 2
//            }
//          ],
//          "tPower": 40
//        }
//      ]
//    }
//  ]
//}

