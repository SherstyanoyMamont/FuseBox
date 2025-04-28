using FuseBox.App.Models;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using static System.Collections.Specialized.BitVector32;

namespace FuseBox
{
    // Сервисный класс, который содержит логику для работы с объектами конфигурации
    public class ConfigurationService
    {
        public List<Component> shieldModuleSet = new();
        public List<Port> ports;
        public List<RCD> uzos = new();

        public Project project;
        public FuseBoxUnit fuseBox;

        public decimal WireSection;
        
        public ConfigurationService(Project Project) 
        {
            project = Project;
            fuseBox = project.FuseBox;
            ports = Port.CreateStandardPorts(WireSection);
        }

        // Создаем/Модифицируем объект проекта
        public void GenerateConfiguration()
        {
            Console.WriteLine("▶ Начинаем GenerateConfiguration");

            try
            {
                Console.WriteLine("🔍 Step 1: ValidateInitialSettings");
                ValidateInitialSettings();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка в ValidateInitialSettings", ex);
            }

            try
            {
                Console.WriteLine("🔍 Step 2: CalculateWireCrossSection");
                CalculateWireCrossSection();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка в CalculateWireCrossSection", ex);
            }

            try
            {
                Console.WriteLine("🔍 Step 3: ConfigureShield");
                ConfigureShield();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка в ConfigureShield", ex);
            }

            try
            {
                Console.WriteLine("🔍 Step 4: Distribute");
                Distribute();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка в Distribute", ex);
            }

            try
            {
                Console.WriteLine("🔍 Step 5: CreateConnections");
                CreateConnections();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка в CreateConnections", ex);
            }

            try
            {
                Console.WriteLine("🔍 Step 6: ShieldByLevel");
                ShieldByLevel();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка в ShieldByLevel", ex);
            }

            Console.WriteLine("✅ GenerateConfiguration завершён успешно.");
        }

        // Логика конфигурации устройств...
        public void ConfigureShield()
        {
            List<Port> SelectPorts(params int[] indices) => indices.Select(i => ports[i]).ToList();

            var ports2x2 = SelectPorts(0, 1, 6, 7);
            var ports2   = SelectPorts(1, 7);
            var ports2x2i= SelectPorts(1, 3, 5, 7);
            var ports1_6 = SelectPorts(0, 1, 2, 3, 4, 5);
            var ports1_7 = SelectPorts(0, 1, 2, 3, 4, 5, 7);
            var ports1_8 = SelectPorts(0, 1, 2, 3, 4, 5, 6, 7);
            var ports017 = SelectPorts(0, 1, 7);
            var ports237 = SelectPorts(2, 3, 7);
            var ports457 = SelectPorts(4, 5, 7);

            if (project.InitialSettings.PhasesCount == 1) // Входим в расчеты 1 фазы
            {
                if (fuseBox.MainBreaker) { shieldModuleSet.Add(new Introductory("Introductory", project.InitialSettings.MainAmperage, 2, 35, ports2x2, "P1", Type3PN.P1)); }
                if (fuseBox.SurgeProtection) { shieldModuleSet.Add(new Component("SPD", 100, 2, 65, ports2)); }
                if (fuseBox.LoadSwitch2P) { shieldModuleSet.Add(new Component("LoadSwitch", 63, 2, 35, ports2x2)); }
                if (fuseBox.RailMeter) { shieldModuleSet.Add(new Component("DinRailMeter", 63, 6, 145, ports2x2)); }
                if (fuseBox.FireUZO) { shieldModuleSet.Add(new RCDFire("RCDFire", 63, 2, 75, ports2x2)); }
                if (fuseBox.VoltageRelay) { shieldModuleSet.Add(new Component("VoltageRelay", 16, 2, 40, ports2x2)); }

                if (fuseBox.RailSocket) { shieldModuleSet.Add(new Component("DinRailSocket", 16, 2, 22, ports2)); }
                if (fuseBox.NDiscLine) { shieldModuleSet.Add(new RCD("NDiscLine", 25, 2, 43, ports2, new List<Component>())); }
                if (fuseBox.LoadSwitch) { shieldModuleSet.Add(new Component("LoadSwitch", 63, 2, 35, ports2x2)); }
                if (fuseBox.ModularContactor) { shieldModuleSet.Add(new Contactor("ModularContactor", 100, 4, 25, ports2)); } // !!!
                if (fuseBox.CrossModule) { shieldModuleSet.Add(new Component("CrossBlock", 100, 4, 25, ports2x2)); }
            }
            else // Входим в расчеты 3 фазы
            {
                if (fuseBox.MainBreaker && !fuseBox.Main3PN) { shieldModuleSet.Add(new Introductory("Introductory3p", project.InitialSettings.MainAmperage, 3, 35, ports1_6, "P1", Type3PN.P3)); }
                if (fuseBox.Main3PN && !fuseBox.MainBreaker) { shieldModuleSet.Add(new Introductory("Introductory3pn", project.InitialSettings.MainAmperage, 4, 35, ports1_8, "P1", Type3PN.P3_N)); }
                if (fuseBox.SurgeProtection) { shieldModuleSet.Add(new Component("SPD3", 100, 4, 65, ports2x2i)); }
                if (fuseBox.RailMeter) { shieldModuleSet.Add(new Component("DinRailMeter3p", 63, 6, 145, ports1_8)); }
                if (fuseBox.FireUZO) { shieldModuleSet.Add(new RCDFire("RCDFire", 63, 4, 75, ports1_8)); }
                if (fuseBox.VoltageRelay && !fuseBox.ThreePRelay)
                {                                        
                    shieldModuleSet.Add(new Component("VoltageRelay1", 16, 2, 40, ports017));
                    shieldModuleSet.Add(new Component("VoltageRelay2", 16, 2, 40, ports237));
                    shieldModuleSet.Add(new Component("VoltageRelay3", 16, 2, 40, ports457));                   
                }
                if (fuseBox.ThreePRelay && !fuseBox.VoltageRelay) { shieldModuleSet.Add(new Component("VoltageRelay", 16, 2, 60, ports1_7)); }
                if (fuseBox.RailSocket) { shieldModuleSet.Add(new Component("DinRailSocket", 16, 2, 22)); }
                if (fuseBox.ModularContactor) { shieldModuleSet.Add(new Contactor("ModularContactor", 100, 4, 25)); } // !!!
                if (fuseBox.CrossModule) { shieldModuleSet.Add(new Component("CrossBlock", 100, 4, 25, ports1_8)); }       // CrossModule? 4 slots?

                
            }
        }
        
        
        public void CalculateWireCrossSection()
        {
            // Стандартные сечения проводов (в мм²) и их предельный ток (в А) для меди
            var copperWireTable = new Dictionary<double, double>
            {
                { 1.5, 18 }, { 2.5, 25 }, { 4, 32 }, { 6, 40 }, { 10, 63 }, { 16, 80 }
            };

            // Поиск минимального сечения, подходящего под заданный ток
            foreach (var wire in copperWireTable)
            {
                if (project.CalculateTotalPower() <= wire.Value)
                    WireSection = (decimal)wire.Key;

                    //return (decimal)wire.Key; // Возвращаем сечение, соответствующее току
            }

            // Если ток выше максимального в таблице — требуется индивидуальный расчёт
            //throw new ArgumentException("Требуется кабель большего сечения, рассчитайте вручную.");

            // Сечения медного кабеля для прокладки проводки по дому/ квартире:
            // автомат C10, сечение кабеля 1,5 мм2 для освещения
            // автомат C16, сечение кабеля 2,5 мм2 для розеток
            // автомат C32, сечение кабеля 6,0 мм2 для мощных потребителей
            // Кабель сечением 8 — 10 мм2 для соединения аппаратуры внутри щита. Обычно используется медный кабель типа ВВГнГ плоский трёхжильный монопроволочный.
        }
        // Логика распределения модулей по уровням...
        public void Distribute()
        {
            DistributionService distributionService = new(project, uzos);

            distributionService.DistributeOfConsumers(); // Логика распределения потребителей
            distributionService.DistributeRCDFromLoad(); // Логика распределения УЗО от нагрузки

            shieldModuleSet.AddRange(uzos); // Соеденяем список входных модулей и УЗО
        }

        // Создаем соединение проводами
        public void CreateConnections()
        {
            List<CableConnection> сableConnections = fuseBox.CableConnections;

            // Добавляем SerialNumber ко всем компонентам
            for (int i = 0; i < shieldModuleSet.Count; i++) { shieldModuleSet[i].SerialNumber = i + 1; }

            for (int i = 0; i < shieldModuleSet.Count; i++)                            // Берем каждый компонент
            {
                Component currentComp = shieldModuleSet[i];                            // module - текущий компонент

                for (int port = 0; port < currentComp.Ports.Count; port++)             // Берем каждый разьем компонента
                {
                    Port currentPort = currentComp.Ports[port];                        // currentPort - текущий разьем

                    if (currentPort.portOut == null)                                   // если порт не выходящий то пропускаем 
                        continue;

                    for (int n = i + 1; n < shieldModuleSet.Count; n++)                // Перебираем следующие компоненты по очереди
                    {
                        Component nextModule = shieldModuleSet[n];                     // nextModule - следующий компонент


                        // Если у компонента есть такой же тип выхода, то создаем подключение
                        if (nextModule.Ports.Any(e => e.portOut == currentPort.portOut)) // Есть ли хотя бы один порт у nextModule с таким же типом выхода как у currentPort?
                        {
                            //AddConnection(сableConnections, module.Id, currentPort, n);

                            Position position = new Position(currentComp.SerialNumber, n + 1);
                            Cable cable = new Cable(currentPort.connectorColour, 10); //        !!!!!! Временная замена на 10

                            сableConnections.Add(new CableConnection(cable, position));

                            //currentPort.connectionsCount++;    // Добавляем информацию про колличетво соединений в разьем
                            break;                               // Берем следующий выходной разьем
                        }
                    }
                }
            }
        }

        public void ShieldByLevel()
        {
            int occupiedSlots = 0;
            int currentLevel = 0;
            int shieldWidth = project.InitialSettings.ShieldWidth;

            // Гарантируем наличие первого уровня
            if (fuseBox.ComponentGroups == null || fuseBox.ComponentGroups.Count == 0)
            {
                fuseBox.ComponentGroups = new List<FuseBoxComponentGroup> { new FuseBoxComponentGroup() };
            }


            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                occupiedSlots += (int)shieldModuleSet[i].Slots;

                // Если модуль помещается на уровне
                //if (occupiedSlots < shieldWidth) fuseBox.ComponentGroups[currentLevel].Components.Add(shieldModuleSet[i]);    // модуль помещается на уровне
                
                if (occupiedSlots < shieldWidth)
                {
                    var component = shieldModuleSet[i];


                    fuseBox.ComponentGroups[currentLevel].Components.Add(component);


                    //var group = fuseBox.ComponentGroups[currentLevel];
                    //component.FuseBoxComponentGroup = group; // 🔁 или .FuseBoxComponentGroupId = group.Id, если хочешь вручную
                    //group.Components.Add(component); // Присваиваем группу

                }
                else if (occupiedSlots > shieldWidth)           // модуль не помещается на уровне. 
                {
                    fuseBox.ComponentGroups[currentLevel].Components.Add(new EmptySlot(shieldWidth - (occupiedSlots - (int)shieldModuleSet[i].Slots)));
                    currentLevel++;
                    fuseBox.ComponentGroups.Add(new FuseBoxComponentGroup());


                    occupiedSlots = (int)shieldModuleSet[i].Slots;
                    var component = shieldModuleSet[i];
                    fuseBox.ComponentGroups[currentLevel].Components.Add(component);


                    //var group = fuseBox.ComponentGroups[currentLevel];
                    //component.FuseBoxComponentGroup = group; // 🔁 или .FuseBoxComponentGroupId = group.Id, если хочешь вручную
                    //group.Components.Add(component); // Присваиваем группу

                }
                else if (occupiedSlots == shieldWidth)      // Слотов на уровне аккурат равно длине шины
                {
                    var component = shieldModuleSet[i];
                    fuseBox.ComponentGroups[currentLevel].Components.Add(component);


                    //var group = fuseBox.ComponentGroups[currentLevel];
                    //component.FuseBoxComponentGroup = group; // 🔁 или .FuseBoxComponentGroupId = group.Id, если хочешь вручную
                    //group.Components.Add(component); // Присваиваем группу


                    if (shieldModuleSet[i] != shieldModuleSet[^1])
                    {
                        fuseBox.ComponentGroups.Add(new FuseBoxComponentGroup());
                        currentLevel++;
                        occupiedSlots = 0;
                    }
                }

                // Добавляем пустые слоты, если компоненты не поместились в конце
                if (occupiedSlots < shieldWidth && shieldModuleSet[i] == shieldModuleSet[^1])
                    fuseBox.ComponentGroups[currentLevel].Components.Add(new EmptySlot(shieldWidth - occupiedSlots));
            }

            //int serialNumber = 1; // начинаем с 1 для каждой группы

            foreach (var group in fuseBox.ComponentGroups)
            {
                

                foreach (var component in group.Components)
                {
                    //component.SerialNumber = serialNumber++;
                    component.FuseBoxComponentGroup = group;
                }
            }

        }

        // Расчет сечения провода по мощности
        public void ValidateInitialSettings()
        {

        }
        //private void CalculateConnectionCount(){}
    }
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
      "PhasesCount": 3,
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
                "Name": "TV",
                "Amper": 1
              },
              {
                "Name": "Air Conditioner",
                "Amper": 8
              },
              {
                "Name": "Lighting",
                "Amper": 1
              }
            ],
            "tPower": 10
          },
          {
            "Name": "Kitchen",
            "Consumer": [
              {
                "Name": "Refrigerator",
                "Amper": 3
              },
              {
                "Name": "Microwave",
                "Amper": 5
              },
              {
                "Name": "Oven",
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
            "Name": "Bedroom 1",
            "Consumer": [
              {
                "Name": "Heater",
                "Amper": 13
              },
              {
                "Name": "Fan",
                "Amper": 7
              }
            ],
            "tPower": 20
          },
          {
            "Name": "Bathroom",
            "Consumer": [
              {
                "Name": "Water Heater",
                "Amper": 13
              },
              {
                "Name": "Hair Dryer",
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
            "Name": "Office",
            "Consumer": [
              {
                "Name": "Computer",
                "Amper": 2
              },
              {
                "Name": "Printer",
                "Amper": 1
              },
              {
                "Name": "Lighting",
                "Amper": 2
              },
              {
                "Name": "Air Conditioner",
                "Amper": 2
              },
              {
                "Name": "Air Conditioner",
                "Amper": 1
              },
              {
                "Name": "Lighting",
                "Amper": 2
              },
              {
                "Name": "Lighting",
                "Amper": 2
              }
            ],
            "tPower": 12
          }
        ]
      }
    ]
  }



*/