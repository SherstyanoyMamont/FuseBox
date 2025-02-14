using FuseBox;
using FuseBox.App.Models;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using static System.Collections.Specialized.BitVector32;

namespace FuseBox
{
    // Сервисный класс, который содержит логику для работы с объектами конфигурации
    public class ConfigurationService
    {
        public List<Port> ports = new List<Port>();
        public List<Component> shieldModuleSet = new List<Component>();

        // Создаем/Модифицируем объект проекта
        public Project GenerateConfiguration(Project project) // Метод возвращает объект ProjectConfiguration
        {
            //ValidateInitialSettings(input.InitialSettings); // Проверка первичных данных***
            List<RCD> uzos = new List<RCD>();
            List<Fuse> AVFuses = new List<Fuse>();

            // Расчет параметров устройства электрощита
            FuseBox fuseBox;

            // Данные для входных и выходных портов
            var portData = new (PortIn? portIn, PortOut? portOut, ConnectorColour colour)[]
            {
                               (PortIn.Phase1, PortOut.Phase1, ConnectorColour.Red),
                               (PortIn.Phase2, PortOut.Phase2, ConnectorColour.Orange),
                               (PortIn.Phase3, PortOut.Phase3, ConnectorColour.Grey),
                               (PortIn.Zero,   PortOut.Zero,   ConnectorColour.Blue)
            };

            // Расчет сечения проводов для щитка
            double TotoalPower = project.CalculateTotalPower();
            decimal WireSection = Convert.ToDecimal(CalculateWireCrossSection(TotoalPower));

            DistributionService distributionService = new DistributionService();

            // Заполняем список портами
            foreach (var (portIn, portOut, colour) in portData)
            {
                if (portIn.HasValue)
                    ports.Add(new Port(portIn.Value, new Cable(colour, WireSection)));

                if (portOut.HasValue)
                    ports.Add(new Port(portOut.Value, new Cable(colour, WireSection)));
            }

            // Входим в расчеты 1 или 3 фазы
            if (project.InitialSettings.PhasesCount == 1) 
            {
                fuseBox = ConfigureShield(project.FuseBox, project.InitialSettings.MainAmperage);
            }
            else
            {
                fuseBox = ConfigureShield3(project.FuseBox, project.InitialSettings.MainAmperage);
            }

            // Добавляем порты во входную группу (УЗО и автоматы получают порты при создании)
            AddPorts(project);

            // Логика распределения потребителей
            distributionService.DistributeOfConsumers(project.GlobalGrouping, CalculateAllConsumers(project.Floors), AVFuses);

            // Логика распределения УЗО от нагрузки
            distributionService.DistributeRCDFromLoad(project.CalculateTotalPower(), uzos, AVFuses, project.InitialSettings.PhasesCount);
            // ----

            // Соеденяем список входных модулей и УЗО
            shieldModuleSet.AddRange(uzos);

            // Добавляем ID ко всем компонентам
            for (int i = 0; i < shieldModuleSet.Count; i++) { shieldModuleSet[i].Id = i + 1; }

            // Создаем соединения
            CreateConnections(fuseBox.CableConnections);

            // Компонуем щит по уровням
            ShieldByLevel(project, project.FuseBox);

            // Возвращаем новый модифицированный объект
            return new Project
            {
                FuseBox = fuseBox, // Возвращаем настроенный щит
                TotalPower = project.CalculateTotalPower()
            };
        }

        // Логика конфигурации устройств...
        private FuseBox ConfigureShield(FuseBox fuseBox, int MainAmperage)             
        {
            int standardSize = 2;

            if (fuseBox.MainBreaker)      { shieldModuleSet.Add(new Introductory("Introductory",Type3PN.P1, standardSize, MainAmperage, 35, "P1")); }
            if (fuseBox.SurgeProtection)  { shieldModuleSet.Add(new Component   ("SPD",              100,   standardSize, 65     )); }
            if (fuseBox.LoadSwitch2P)     { shieldModuleSet.Add(new Component   ("LoadSwitch",       63,    standardSize, 35     )); }
            if (fuseBox.RailMeter)        { shieldModuleSet.Add(new Component   ("DinRailMeter",     63,    6,            145    )); }
            if (fuseBox.FireUZO)          { shieldModuleSet.Add(new RCDFire     ("RCDFire",          63,    standardSize, 75     )); }
            if (fuseBox.VoltageRelay)     { shieldModuleSet.Add(new Component   ("VoltageRelay",     16,    standardSize, 40     )); }
            if (fuseBox.RailSocket)       { shieldModuleSet.Add(new Component   ("DinRailSocket",    16,    standardSize, 22     )); }
            if (fuseBox.NDiscLine)        { shieldModuleSet.Add(new RCD         ("NDiscLine",        25,    standardSize, 43, new List<BaseElectrical>())); }
            if (fuseBox.LoadSwitch)       { shieldModuleSet.Add(new Component   ("LoadSwitch",       63,    standardSize, 35     )); }
            if (fuseBox.ModularContactor) { shieldModuleSet.Add(new Contactor   ("ModularContactor", 100,   4,            25, fuseBox.Contactor)); } // !!!
            if (fuseBox.CrossModule)      { shieldModuleSet.Add(new Component   ("CrossBlock",       100,   4,            25     )); }

            return fuseBox;
        }
        private FuseBox ConfigureShield3(FuseBox fuseBox, int MainAmperage)
        {
            // Настройки опцыонных автоматов \\
            if (fuseBox.MainBreaker)
            {
                // Если да, то добавляем 3 фазы + ноль
                if (fuseBox.Main3PN)
                {
                    shieldModuleSet.Add(new Introductory("Introductory 3P+N", Type3PN.P3_N, 2, MainAmperage, 35, "P1"));
                }                                                                                            
                else                                                                                         
                {                                                                                            
                    shieldModuleSet.Add(new Introductory("Introductory 3P",   Type3PN.P3,   2, MainAmperage, 35, "P1"));
                }
            }
            if (fuseBox.SurgeProtection)  { shieldModuleSet.Add(new Component("SPD",              100, 2, 65)); }
            if (fuseBox.RailMeter)        { shieldModuleSet.Add(new Component("DinRailMeter",     63,  6, 145)); }
            if (fuseBox.FireUZO)          { shieldModuleSet.Add(new RCDFire  ("RCDFire",          63,  2, 75)); }
            if (fuseBox.VoltageRelay)
            {
                if (fuseBox.ThreePRelay)
                {
                    shieldModuleSet.Add(new Component("VoltageRelay",  16,  2, 60));
                                                                           
                }                                                          
                else                                                       
                {                                                          
                    shieldModuleSet.Add(new Component("VoltageRelay1", 16,  2, 40));
                    shieldModuleSet.Add(new Component("VoltageRelay2", 16,  2, 40));
                    shieldModuleSet.Add(new Component("VoltageRelay3", 16,  2, 40));
                }
            }
            if (fuseBox.RailSocket)       { shieldModuleSet.Add(new Component("DinRailSocket",    16,  3, 22)); }
            if (fuseBox.ModularContactor) { shieldModuleSet.Add(new Contactor("ModularContactor", 100, 4, 25, fuseBox.Contactor)); } // !!!
            if (fuseBox.CrossModule)      { shieldModuleSet.Add(new Component("CrossBlock",       100, 4, 25)); }       // CrossModule? 4 slots?

            return fuseBox;
        }

        // Добавляем порты
        public void AddPorts(Project project)
        {
            if (project.InitialSettings.PhasesCount == 1)
            {
                foreach (Component component in shieldModuleSet)
                {
                    if (component.Name == "SPD" || component.Name == "NDiscLine")
                    {
                        component.Ports.Add(ports[1]);
                        component.Ports.Add(ports[7]);
                    }
                    else if (component.Name == "DinRailSocket") { }
                    else if (component.Name == "ModularContactor")
                    {
                        component.Ports.Add(ports[1]);
                        component.Ports.Add(ports[7]);
                    }
                    else
                    {
                        component.Ports.Add(ports[0]);
                        component.Ports.Add(ports[1]);
                        component.Ports.Add(ports[6]);
                        component.Ports.Add(ports[7]);
                    }
                }
            }
            else
            {
                foreach (Component component in shieldModuleSet)
                {
                    if (component.Name == "SPD")
                    {
                        component.Ports.Add(ports[1]);
                        component.Ports.Add(ports[3]);
                        component.Ports.Add(ports[5]);
                        component.Ports.Add(ports[7]);
                    }
                    else if (component.Name == "Introductory 3P")
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            component.Ports.Add(ports[i]);
                        }
                    }
                    else if (component.Name == "VoltageRelay")
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            component.Ports.Add(ports[i]);
                        }
                    }
                    else if (component.Name == "VoltageRelay1")
                    {
                        component.Ports.Add(ports[0]);
                        component.Ports.Add(ports[1]);
                        component.Ports.Add(ports[6]);
                    }
                    else if (component.Name == "VoltageRelay2")
                    {
                        component.Ports.Add(ports[2]);
                        component.Ports.Add(ports[3]);
                        component.Ports.Add(ports[6]);
                    }
                    else if (component.Name == "VoltageRelay3")
                    {
                        component.Ports.Add(ports[4]);
                        component.Ports.Add(ports[5]);
                        component.Ports.Add(ports[6]);
                    }
                    else if (component.Name == "DinRailSocket" || component.Name == "ModularContactor") { }
                    else
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            component.Ports.Add(ports[i]);
                        }
                    }
                }
            }
        }

        // Логика распределения модулей по уровням...
        public void ShieldByLevel(Project project, FuseBox fuseBox)
        {            
            int occupiedSlots = 0;
            int currentLevel = 0;
            int shieldWidth = project.InitialSettings.ShieldWidth;

            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                occupiedSlots += (int)shieldModuleSet[i].Slots;

                if (occupiedSlots < shieldWidth) fuseBox.Components[currentLevel].Add(shieldModuleSet[i]);    // модуль помещается на уровне

                else if (occupiedSlots > shieldWidth)           // модуль не помещается на уровне. 
                {
                    fuseBox.Components[currentLevel].Add(new EmptySlot(shieldWidth - (occupiedSlots - (int)shieldModuleSet[i].Slots)));
                    currentLevel++;
                    fuseBox.Components.Add(new List<BaseElectrical>());

                    occupiedSlots = (int)shieldModuleSet[i].Slots;
                    fuseBox.Components[currentLevel].Add(shieldModuleSet[i]);
                }
                else if (occupiedSlots == shieldWidth)      // Слотов на уровне аккурат равно длине шины
                {
                    fuseBox.Components[currentLevel].Add(shieldModuleSet[i]);                  
                    if (shieldModuleSet[i] != shieldModuleSet[^1])
                    {
                        fuseBox.Components.Add(new List<BaseElectrical>());
                        currentLevel++;
                        occupiedSlots = 0;
                    }                   
                }
                if (occupiedSlots < shieldWidth && shieldModuleSet[i] == shieldModuleSet[^1])
                    fuseBox.Components[currentLevel].Add(new EmptySlot(shieldWidth - occupiedSlots));
            }
            //ShieldWithInterSlots(project);
        }
        public List<BaseElectrical> CalculateAllConsumers(List<Floor> floors)
        {
            List<BaseElectrical> AllConsumers = new List<BaseElectrical>();
            foreach (var floor in floors)
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

        // Расчет сечения провода по мощности
        public double CalculateWireCrossSection(double currentAmps)
        {
            // Стандартные сечения проводов (в мм²) и их предельный ток (в А) для меди
            var copperWireTable = new Dictionary<double, double>
            {
                { 1.5, 18 }, { 2.5, 25 }, { 4, 32 }, { 6, 40 }, { 10, 63 },
                { 16, 80 }
            };

            // Поиск минимального сечения, подходящего под заданный ток
            foreach (var wire in copperWireTable)
            {
                if (currentAmps <= wire.Value)
                {
                    return wire.Key; // Возвращаем сечение, соответствующее току
                }
            }

            // Если ток выше максимального в таблице — требуется индивидуальный расчёт
            throw new ArgumentException("Требуется кабель большего сечения, рассчитайте вручную.");


            // Сечения медного кабеля для прокладки проводки по дому/ квартире:
            // автомат C10, сечение кабеля 1,5 мм2 для освещения
            // автомат C16, сечение кабеля 2,5 мм2 для розеток
            // автомат C32, сечение кабеля 6,0 мм2 для мощных потребителей
            // Кабель сечением 8 — 10 мм2 для соединения аппаратуры внутри щита. Обычно используется медный кабель типа ВВГнГ плоский трёхжильный монопроволочный.
        }

        // Расчет входного автомата по мощности
        public static int CalculateMainBreaker(double currentAmps)
        {
            // Стандартные номиналы автоматов в амперах (по ГОСТ, IEC)
            int[] standardBreakers = { 2, 3,  4, 6, 10, 16, 20, 25, 32, 40, 50, 63, 80, 100, 125, 160 };

            // Ищем минимальный номинал, который больше или равен требуемому току
            foreach (var breaker in standardBreakers)
            {
                if (currentAmps <= breaker)
                {
                    return breaker;
                }
            }

            // Если требуется автомат большего номинала — выбрасываем исключение
            throw new ArgumentException("Требуется автомат с номиналом выше 125А, уточните расчёт.");
        }

        // Создаем соединение проводами
        public void CreateConnections(List<Connection> сableConnections)
        {
            int nextModule = 1;
            int previousModule = -1;

            for (int i = 0; i < shieldModuleSet.Count; i++)                                     // Берем каждый компонент
            {
                for (int port = 0; port < shieldModuleSet[i].Ports.Count; port++)               // Берем каждый разьем компонента
                {
                    if (shieldModuleSet[i].Ports[port].portOut != 0)                            // Но скипаем входы
                    {
                        for (int n = shieldModuleSet[i].Id + nextModule; n < shieldModuleSet.Count() + nextModule; n++)    // Перебираем следующие компоненты по очереди
                        {
                            // Если у компонента есть такой же тип выхода, то есть и подходящий вход
                            if (shieldModuleSet[n + previousModule].Ports.Any(e => e.portOut.ToString() == shieldModuleSet[i].Ports[port].portOut.ToString()))
                            {
                                // Создаем соединение
                                AddConnection(сableConnections, shieldModuleSet[i].Id, shieldModuleSet[i].Ports[port], n);

                                // Берем следующий выходной разьем
                                break;
                            }
                            else
                            {
                                // Есть ли у него подходящий вход?
                                if (shieldModuleSet[n + previousModule].Ports.Any(e => e.PortIn.ToString() == shieldModuleSet[i].Ports[port].portOut.ToString()))
                                {
                                    AddConnection(сableConnections, shieldModuleSet[i].Id, shieldModuleSet[i].Ports[port], n);

                                    // Продолжаем подключение из того-же разьема
                                }
                            }
                        }
                    }
                }
            }
        }
        public void AddConnection(List<Connection> CableConnections, int componentId, Port port, int indexFinish)
        {
            Position connectionIds = new Position(componentId, indexFinish);
            CableConnections.Add(new Connection(port.cableType, connectionIds));

            // Добавляем информацию про колличетво соединений в разьем
            port.connectionsCount += 1;
        }
    }
}

/*
public void ShieldNewLVL(Project project, List<Component> shieldModuleSet, List<Fuse> AVFuses) // Участь слоты автоматов у УЗО
{
    int shieldWidth = project.InitialSettings.ShieldWidth;       // количество слотов на каждом уровне
    int remainingSlots = shieldWidth;                            // количество оставшихся слотов на текущем уровне
    project.FuseBox.Components.Add(new List<BaseElectrical>());  // создаем первый уровень
    int levelIndex = 0; // индекс списка в project.FuseBox.Components указывающее на текущий уровень

    for (int i = 0; i < shieldModuleSet.Count; i++)
    {
        if (shieldModuleSet[i].Slots < remainingSlots)
        {
            project.FuseBox.Components[levelIndex].Add(shieldModuleSet[i]); // есть место - добавляем елемент на уровень
            remainingSlots -= shieldModuleSet[i].Slots;                     // уменьшаем количество оставшихся слотов
        }
        else if (shieldModuleSet[i].Slots == remainingSlots)
        {
            project.FuseBox.Components[levelIndex].Add(shieldModuleSet[i]); // если елемент помещается как раз в оставшиеся слоты
            project.FuseBox.Components.Add(new List<BaseElectrical>());
            levelIndex++;

            remainingSlots = shieldWidth; // новый уровень, снова все слоты доступны
        }
        else if (shieldModuleSet[i].Slots > remainingSlots)
        {
            // текущий елемент не поместился на уровень, заполняем оставшееся место
            project.FuseBox.Components[levelIndex].Add(new Component("{empty space}", 0, remainingSlots, 0, 0));
            project.FuseBox.Components.Add(new List<BaseElectrical>());     // создаем новый уровень
            levelIndex++;                                                   // переходим на этот новый уровень

            project.FuseBox.Components[levelIndex].Add(shieldModuleSet[i]); // добавляем текущий элемент не поместившийся на новый уровень
            remainingSlots = shieldWidth - shieldModuleSet[i].Slots;        // новый уровень, снова все слоты доступны но минус слоты текущего элемента
        }

        //public List<Component> ShieldByLevel(Project project, List<Component> shieldModuleSet) // Логика распределения модулей по уровням
        //{

        //    // Логика распределения модулей по уровням

        //    double countOfSlots = 0;

        //    // Вычисляем общее количество слотов для Щитовой панели
        //    for (int i = 0; i < shieldModuleSet.Count; i++)
        //    {
        //        countOfSlots += shieldModuleSet[i].Slots;
        //    }

        //    var countOfDINLevels = Math.Ceiling(countOfSlots / project.InitialSettings.ShieldWidth); //Количество уровней ДИН рейки в Щите

        //    // Инициализируем списки каждого уровня щита
        //    for (int i = 0; i < countOfDINLevels; i++)
        //    {
        //        project.Shield.Fuses.Add(new List<Component>());
        //    }

        //    project.Shield.DINLines = (int)countOfDINLevels; // Запись в поле объекта количество уровней в щите (Как по мне лишнее)

        //    int startPos = 0;
        //    int endPos = 0;
        //    int occupiedSlots = 0;
        //    int currentLevel = 0;
        //    int emptySlotsOnDINLevel = 0;
        //    int shieldWidth = project.InitialSettings.ShieldWidth;

        //    for (int i = 0; i < shieldModuleSet.Count; i++)
        //    {
        //        occupiedSlots += (int)shieldModuleSet[i].Slots;

        //        if (occupiedSlots >= shieldWidth)
        //        {
        //            if ((occupiedSlots > shieldWidth) && (occupiedSlots != shieldWidth))
        //            {
        //                emptySlotsOnDINLevel = shieldWidth - (occupiedSlots - (int)shieldModuleSet[i].Slots);
        //                shieldModuleSet.Insert(i, new Module("{empty space}", 0, emptySlotsOnDINLevel, false, 0, false, "")); // i-ый элемент становится i+1, а пустой - i-ым
        //                endPos = i + 1;
        //                project.Shield.Fuses[currentLevel].AddRange(shieldModuleSet.GetRange(startPos, endPos - startPos));
        //                startPos = endPos;
        //                occupiedSlots = 0;
        //                currentLevel++;
        //                continue;
        //            }
        //            if (occupiedSlots == shieldWidth)
        //            {
        //                endPos = i + 1;
        //                project.Shield.Fuses[currentLevel].AddRange(shieldModuleSet.GetRange(startPos, endPos - startPos));
        //                startPos = endPos;
        //                occupiedSlots = 0;
        //                currentLevel++;
        //            }

        //        }
        //        if (i == shieldModuleSet.Count - 1 && occupiedSlots != shieldWidth)
        //        {
        //            endPos = i + 1;
        //            project.Shield.Fuses[currentLevel].AddRange(shieldModuleSet.GetRange(startPos, endPos - startPos));
        //            project.Shield.Fuses[currentLevel].Add(new Module("{empty space}", 0, shieldWidth - occupiedSlots, false, 0, false, ""));
        //            currentLevel++;

        //        }
        //        if (currentLevel > countOfDINLevels) break;

        //    }
        //    return shieldModuleSet;
        //}

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
              "Amper": 13
            },
            {
              "id": 8,
              "name": "Fan",
              "Amper": 7
            }
          ],
          "tPower": 20
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
            },
            {
              "id": 11,
              "name": "Air Conditioner",
              "Amper": 2
            },
            {
              "id": 12,
              "name": "Air Conditioner",
              "Amper": 1
            },
            {
              "id": 13,
              "name": "Lighting",
              "Amper": 2
            },
            {
              "id": 13,
              "name": "Lighting",
              "Amper": 2
            }
          ],
          "tPower": 12
        }
      ]
    }
  ]
}

