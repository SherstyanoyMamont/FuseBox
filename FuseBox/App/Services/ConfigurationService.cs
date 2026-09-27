using FuseBox.App.Factorys;
using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using FuseBox.App.Services.Providers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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
        private readonly IEnumerable<IShieldConfigurationRule> singlePhaseRules;
        private readonly IEnumerable<IShieldConfigurationRule> threePhaseRules;
        private readonly IProjectSettings settingsProvider;
        private readonly IComponentFactory componentFactory;
        private readonly IFuseBoxManager fuseBoxManager;
        private readonly IConnectionFactory connectionFactory;
        private readonly IProjectGrouping projectGrouping;
        private readonly IConsumerProvider consumerProvider;
        private readonly IDistributionService distributionService;

        public List<Component> shieldModuleSet = new();

        public ConfigurationService(IProjectSettings settingsProvider, IComponentFactory componentFactory, IFuseBoxManager fuseBoxManager,
            IConnectionFactory connectionFactory, IProjectGrouping projectGrouping, IConsumerProvider consumerProvider,
            IEnumerable<IShieldConfigurationRule> singlePhaseRules, IEnumerable<IShieldConfigurationRule> threePhaseRules, IDistributionService distributionService)
        {
            this.singlePhaseRules = singlePhaseRules;
            this.threePhaseRules = threePhaseRules;
            this.settingsProvider = settingsProvider;
            this.componentFactory = componentFactory;
            this.fuseBoxManager = fuseBoxManager;
            this.connectionFactory = connectionFactory;
            this.projectGrouping = projectGrouping;
            this.consumerProvider = consumerProvider;
            this.distributionService = distributionService;
        }

        public void AddShieldingCable(Cable cable, Position position)
        {
            fuseBoxManager.AddConnection(cable, position);
        }

        public void AddComponentOnLevel(int currentLevel, Component component)
        {
            fuseBoxManager.AddComponentOnLevel(currentLevel, component);
        }

        // Создаем/Модифицируем объект проекта
        public void GenerateConfiguration()
        {
            Console.WriteLine("▶ Начинаем GenerateConfiguration");

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
            //var rules = settingsProvider.GetPhasesCount() == 1
            //? singlePhaseRules
            //: threePhaseRules;

            //foreach (var rule in rules)
            //{
            //    if (rule.ShouldApply(settingsProvider))
            //    {
            //        shieldModuleSet.Add(rule.CreateComponent(componentFactory));
            //    }
            //}



            if (settingsProvider.GetPhasesCount() == 1)
            {
                shieldModuleSet.Add(componentFactory.CreateStartPoint());

                if (settingsProvider.IsIntroductoryEnabled()) { shieldModuleSet.Add(componentFactory.CreateIntroductoryModule()); }

                if (settingsProvider.IsSurgeProtectionEnabled()) { shieldModuleSet.Add(componentFactory.CreateSurgeProtectionModule()); }
                if (settingsProvider.IsLoadSwitchEnabled()) { shieldModuleSet.Add(componentFactory.CreateLoadSwitchModule()); }
                if (settingsProvider.IsRailMeterEnabled()) { shieldModuleSet.Add(componentFactory.CreateRailMeterModule()); } //!!!
                if (settingsProvider.IsFireUZOEnabled()) { shieldModuleSet.Add(componentFactory.CreateFireUZOModule()); }
                if (settingsProvider.IsVoltageRelayEnabled()) { shieldModuleSet.Add(componentFactory.CreateVoltageRelayModule()); }

                if (settingsProvider.IsRailSocketEnabled()) { shieldModuleSet.Add(componentFactory.CreateRailSocketModule()); }
                if (settingsProvider.IsNDiscLineEnabled()) { shieldModuleSet.Add(componentFactory.CreateNDiscLineModule()); }
                if (settingsProvider.IsLoadSwitchEnabled()) { shieldModuleSet.Add(componentFactory.CreateLoadSwitchModule()); }
                if (settingsProvider.IsModularContactorEnabled()) { shieldModuleSet.Add(componentFactory.CreateModularContactorModule()); }
                if (settingsProvider.IsCrossModuleEnabled()) { shieldModuleSet.Add(componentFactory.CreateCrossBlockModule()); }
            }
            else if (settingsProvider.GetPhasesCount() == 3) // Входим в расчеты 3 фазы
            {
                shieldModuleSet.Add(componentFactory.CreateStartPoint3p());

                if (settingsProvider.IsIntroductoryEnabled())
                {
                    if (settingsProvider.IsIntroductory3pnEnabled())
                    {
                        shieldModuleSet.Add(
                            componentFactory.CreateIntroductory3pnModule());
                    }
                    else
                    {
                        shieldModuleSet.Add(
                            componentFactory.CreateIntroductory3pModule());
                    }
                }
                if (settingsProvider.IsSPD3Enabled()) { shieldModuleSet.Add(componentFactory.CreateSPD3Module()); }
                if (settingsProvider.IsDinRailMeter3pEnabled()) { shieldModuleSet.Add(componentFactory.CreateDinRailMeter3pModule()); }
                if (settingsProvider.IsRCDFireEnabled()) { shieldModuleSet.Add(componentFactory.CreateRCDFire3pModule()); }
                if (settingsProvider.IsVoltageRelayEnabled() && !settingsProvider.IsVoltageRelay3Enabled())
                {
                    shieldModuleSet.Add(componentFactory.CreateVoltageRelayModule());
                    shieldModuleSet.Add(componentFactory.CreateVoltageRelay2Module());
                    shieldModuleSet.Add(componentFactory.CreateVoltageRelay3Module());
                }
                if (settingsProvider.IsVoltageRelay3Enabled() && !settingsProvider.IsVoltageRelayEnabled()) { shieldModuleSet.Add(componentFactory.CreateVoltageRelayModule()); }
                if (settingsProvider.IsDinRailSocketEnabled()) { shieldModuleSet.Add(componentFactory.CreateRailSocketModule()); }
                if (settingsProvider.IsModularContactor3pEnabled()) { shieldModuleSet.Add(componentFactory.CreateModularContactorModule()); } // !!!
                if (settingsProvider.IsCrossBlockEnabled()) { shieldModuleSet.Add(componentFactory.CreateCrossBlockModule()); }       // CrossModule? 4 slots?
            }
            else new Exception("Unexpected phase type!");
        }

        // Логика распределения модулей по уровням...
        public void Distribute()
        {
            distributionService.DistributeOfConsumers(); // Логика распределения потребителей
            distributionService.DistributeRCDFromLoad(); // Логика распределения УЗО от нагрузки

            List<Component> RSDplusAV = new List<Component>(); // Список входных модулей и УЗО
            List<RCD> RSD = distributionService.GetDistributedRCDModules();

            foreach (var rcd in RSD)
            {
                RSDplusAV.Add(rcd); // Добавляем УЗО в список
                if (rcd.Electricals != null)
                {
                    foreach (var av in rcd.Electricals)
                    {
                        RSDplusAV.Add(av); // Добавляем АВ в список
                    }
                }
            }

            shieldModuleSet.AddRange(RSDplusAV); // Соеденяем список входных модулей и УЗО

            //shieldModuleSet.AddRange(distributionService.GetDistributedRCDModules());
        }

        // Создаем соединение проводами
        public void CreateConnections()
        {
            if (shieldModuleSet.Count == 0)
                throw new InvalidOperationException("Список устройств пуст.");

            if (shieldModuleSet.Distinct().Count() != shieldModuleSet.Count)
            {
                throw new InvalidOperationException(
                    "Один экземпляр компонента добавлен в схему несколько раз.");
            }

            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                shieldModuleSet[i].SerialNumber = i + 1;
            }

            string NormalizeName(Component component)
            {
                return (component.Name ?? "")
                    .Replace(" ", "")
                    .ToLowerInvariant();
            }

            bool IsStartPoint(Component component)
            {
                string name = NormalizeName(component);

                return name == "startpoint" ||
                       name == "startpoint3p";
            }

            bool IsSupplyChannel(string? channel)
            {
                return channel == "Phase1" ||
                       channel == "Phase2" ||
                       channel == "Phase3" ||
                       channel == "Zero";
            }

            string ColourFor(string channel)
            {
                return channel switch
                {
                    "Phase1" => "Red",
                    "Phase2" => "Orange",
                    "Phase3" => "Grey",
                    "Zero" => "Blue",
                    _ => throw new InvalidOperationException(
                        $"Неизвестный канал: {channel}")
                };
            }

            // В текущей модели эти устройства являются ответвлениями:
            // они получают питание, но не становятся источником общей магистрали.
            bool IsBranch(Component component)
            {
                string name = NormalizeName(component);

                return name == "dinrailsocket" ||
                       name == "ndiscline";
            }

            var planned = new List<(
                int Start,
                int Finish,
                string Colour)>();

            var unique = new HashSet<(
                int Start,
                int Finish,
                string Colour)>();

            void AddConnection(Component start, Component finish, string channel)
            {
                if (ReferenceEquals(start, finish))
                {
                    throw new InvalidOperationException(
                        "Нельзя подключить компонент к самому себе.");
                }

                var connection = (
                    Start: start.SerialNumber,
                    Finish: finish.SerialNumber,
                    Colour: ColourFor(channel));

                if (unique.Add(connection))
                {
                    planned.Add(connection);
                }
            }

            var supply = new Dictionary<string, Component>();

            Component GetSupply(string channel, Component target)
            {
                if (!supply.TryGetValue(channel, out var source))
                {
                    throw new InvalidOperationException(
                        $"Для {target.Name} №{target.SerialNumber} " +
                        $"не найден источник {channel}.");
                }

                return source;
            }

            var incoming = shieldModuleSet
                .Where(component =>
                    component is not RCD &&
                    component is not Fuse &&
                    component is not EmptySlot)
                .ToList();

            if (incoming.Count(component => IsStartPoint(component)) != 1 ||
                !IsStartPoint(incoming[0]))
            {
                throw new InvalidOperationException(
                    "Общая часть схемы должна начинаться с одного StartPoint.");
            }

            foreach (var component in incoming)
            {
                var outputs = component.Ports
                    .Select(port => port.portOut)
                    .Where(IsSupplyChannel)
                    .Select(channel => channel!)
                    .Distinct()
                    .ToList();

                if (IsStartPoint(component))
                {
                    foreach (var channel in outputs)
                    {
                        supply[channel] = component;
                    }

                    continue;
                }

                // В существующих пресетах часть питаемых устройств имеет
                // только portOut. Пока учитываем обе записи как описание каналов.
                var channels = component.Ports
                    .SelectMany(port => new[] { port.PortIn, port.portOut })
                    .Where(IsSupplyChannel)
                    .Select(channel => channel!)
                    .Distinct()
                    .ToList();

                if (channels.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"У {component.Name} отсутствует описание каналов питания.");
                }

                foreach (var channel in channels)
                {
                    AddConnection(
                        GetSupply(channel, component),
                        component,
                        channel);
                }

                if (!IsBranch(component))
                {
                    foreach (var channel in outputs)
                    {
                        supply[channel] = component;
                    }
                }
            }

            var owners = new Dictionary<Fuse, RCD>();

            foreach (var rcd in shieldModuleSet.OfType<RCD>())
            {
                var phaseChannels = rcd.Ports
                    .Select(port => port.portOut)
                    .Where(channel =>
                        channel == "Phase1" ||
                        channel == "Phase2" ||
                        channel == "Phase3")
                    .Distinct()
                    .ToList();

                if (phaseChannels.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"У RCD №{rcd.SerialNumber} должна быть одна назначенная фаза.");
                }

                string phase = phaseChannels[0]!;

                if (settingsProvider.GetPhasesCount() == 1 &&
                    phase != "Phase1")
                {
                    throw new InvalidOperationException(
                        "Однофазному RCD назначена фаза L2 или L3.");
                }

                // Питание RCD берётся из общей части щита.
                AddConnection(GetSupply(phase, rcd), rcd, phase);
                AddConnection(GetSupply("Zero", rcd), rcd, "Zero");

                if (rcd.Electricals.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"RCD №{rcd.SerialNumber} не содержит автоматов.");
                }

                foreach (var child in rcd.Electricals)
                {
                    if (child is not Fuse breaker)
                    {
                        throw new InvalidOperationException(
                            $"RCD №{rcd.SerialNumber} содержит компонент, " +
                            "который не является автоматом.");
                    }

                    if (!shieldModuleSet.Contains(breaker))
                    {
                        throw new InvalidOperationException(
                            "Автомат RCD отсутствует в общем списке схемы.");
                    }

                    if (!owners.TryAdd(breaker, rcd))
                    {
                        throw new InvalidOperationException(
                            $"Автомат №{breaker.SerialNumber} назначен нескольким RCD.");
                    }

                    if (breaker.Electricals.Any(consumer =>
                        consumer.RcdMilliAmps != rcd.Capacity))
                    {
                        throw new InvalidOperationException(
                            $"Параметры защиты автомата №{breaker.SerialNumber} " +
                            "не соответствуют его RCD.");
                    }

                    // Автомат получает фазу своего RCD.
                    AddConnection(rcd, breaker, phase);
                }
            }

            foreach (var breaker in shieldModuleSet.OfType<Fuse>())
            {
                if (!owners.ContainsKey(breaker))
                {
                    throw new InvalidOperationException(
                        $"Автомат №{breaker.SerialNumber} не назначен ни одному RCD.");
                }
            }

            // Записываем результат только после проверки всей структуры.
            fuseBoxManager.ClearConnections();

            foreach (var connection in planned)
            {
                var position = connectionFactory.CreatePositionModule(
                    connection.Start,
                    connection.Finish);

                // Сечение 10 сохранено из текущей реализации.
                // Это заглушка приложения, а не результат подбора кабеля.
                var cable = connectionFactory.CreateCableModule(
                    connection.Colour,
                    10);

                AddShieldingCable(cable, position);
            }
        }

        public void ShieldByLevel()
        {
            int shieldWidth = settingsProvider.GetShieldWidth();

            if (shieldWidth <= 0)
                throw new ArgumentException("Ширина щита должна быть положительной.");

            var groups = fuseBoxManager.GetComponentGroups();

            // Здесь размещается заново рассчитанный, ещё не сохранённый проект.
            groups.Clear();

            var blocks = new List<List<Component>>();
            var assigned = new HashSet<Component>();

            foreach (var component in shieldModuleSet)
            {
                if (component is EmptySlot)
                    continue;

                if (component is Fuse)
                    continue;

                var block = new List<Component> { component };

                if (!assigned.Add(component))
                {
                    throw new InvalidOperationException(
                        "Повтор компонента при размещении.");
                }

                if (component is RCD rcd)
                {
                    foreach (var child in rcd.Electricals)
                    {
                        if (child is not Fuse ||
                            !shieldModuleSet.Contains(child) ||
                            !assigned.Add(child))
                        {
                            throw new InvalidOperationException(
                                "Неверная принадлежность автоматов при размещении.");
                        }

                        block.Add(child);
                    }
                }

                blocks.Add(block);
            }

            if (shieldModuleSet
                .Where(component => component is not EmptySlot)
                .Any(component => !assigned.Contains(component)))
            {
                throw new InvalidOperationException(
                    "Часть устройств не попала в блоки размещения.");
            }

            int currentLevel = -1;
            int occupiedSlots = 0;

            void StartRow()
            {
                fuseBoxManager.AddComponentGroup();
                currentLevel++;
                occupiedSlots = 0;
            }

            void FillRemainingSpace()
            {
                int freeSlots = shieldWidth - occupiedSlots;

                if (freeSlots > 0)
                {
                    AddComponentOnLevel(
                        currentLevel,
                        componentFactory.CreateEmptySlotModule(freeSlots));
                }
            }

            foreach (var block in blocks)
            {
                foreach (var component in block)
                {
                    bool startPoint =
                        component.Name == "StartPoint" ||
                        component.Name == "StartPoint3p";

                    if (component.Slots < 0 ||
                        (!startPoint && component.Slots == 0))
                    {
                        throw new InvalidOperationException(
                            $"Некорректная ширина устройства {component.Name}.");
                    }
                }

                int blockWidth = block.Sum(component => component.Slots);

                if (blockWidth > shieldWidth)
                {
                    throw new ArgumentException(
                        $"Группа {block[0].Name} занимает {blockWidth} слотов, " +
                        $"но ширина ряда — {shieldWidth}. Увеличьте ширину щита.");
                }

                if (currentLevel < 0)
                {
                    StartRow();
                }

                if (occupiedSlots + blockWidth > shieldWidth)
                {
                    FillRemainingSpace();
                    StartRow();
                }

                foreach (var component in block)
                {
                    AddComponentOnLevel(currentLevel, component);
                }

                occupiedSlots += blockWidth;
            }

            if (currentLevel >= 0)
            {
                FillRemainingSpace();
            }

            if (groups.Any(group => group.Components.Count == 0))
            {
                throw new InvalidOperationException(
                    "После размещения остался пустой ряд.");
            }

            fuseBoxManager.BindComponentsToGroups();
        }

        //public void ReturnAVToMainList()
        //{
        //    // Возвращаем все АВ в общий список
        //    foreach (var group in fuseBoxManager.GetComponentGroups())
        //    {
        //        foreach (var component in group.Components)
        //        {
        //            if (component is RCD)
        //            {
        //                // Добавляем обратно в общий список
        //                shieldModuleSet.Add(component);
        //            }
        //        }
        //    }

        //    var flatList = new List<Component>();

        //    foreach (var rcd in fuseBoxManager.GetComponentGroups())
        //    {
        //        flatList.Add(rcd); // сначала сам RCD
        //        if (rcd.Electrical != null)
        //        {
        //            flatList.AddRange(rcd.Electrical); // затем все его AV-компоненты
        //        }
        //    }

        //}
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


            //List<Port> SelectPorts(params int[] indices) => indices.Select(i => ports[i]).ToList();

            //var ports2x2 = SelectPorts(0, 1, 6, 7);
            //var ports2 = SelectPorts(1, 7);
            //var ports2x2i = SelectPorts(1, 3, 5, 7);
            //var ports1_6 = SelectPorts(0, 1, 2, 3, 4, 5);
            //var ports1_7 = SelectPorts(0, 1, 2, 3, 4, 5, 7);
            //var ports1_8 = SelectPorts(0, 1, 2, 3, 4, 5, 6, 7);
            //var ports017 = SelectPorts(0, 1, 7);
            //var ports237 = SelectPorts(2, 3, 7);
            //var ports457 = SelectPorts(4, 5, 7);


            //Dictionary<string, Component> phaseOne = new Dictionary<string, Component>
            //{
            //    { "Introductory",     new Introductory(  "Introductory",      settingsProvider.GetMainAmperage(), 2, 35, ports2x2, "P1", Type3PN.P1)},
            //    { "SPD",              new Component(     "SPD",               100, 2, 65,   ports2  )},
            //    { "LoadSwitch",       new Component(     "LoadSwitch",        63, 2, 35,    ports2x2)},
            //    { "DinRailMeter",     new Component(     "DinRailMeter",      63, 6, 145,   ports2x2)},
            //    { "RCDFire",          new RCDFire  (     "RCDFire",           63, 2, 75,    ports2x2)},
            //    { "VoltageRelay",     new Component(     "VoltageRelay",      16, 2, 40,    ports2x2)},                                                                        
            //    { "DinRailSocket",    new Component(     "DinRailSocket",     16, 2, 22,    ports2  )},
            //    { "NDiscLine",        new RCD      (     "NDiscLine",         25, 2, 43,    ports2, new List<Component>()) },
            //    { "LoadSwitch",       new Component(     "LoadSwitch",        63, 2, 35,    ports2x2)},
            //    { "ModularContactor", new Contactor(     "ModularContactor",  100, 4, 25,   ports2  )}, // !!!
            //    { "CrossBlock",       new Component(     "CrossBlock",        100, 4, 25,   ports2x2)},
            //};

            //Dictionary<string, Component> phaseThree = new Dictionary<string, Component>
            //{
            //    { "Introductory3p",   new Introductory(  "Introductory3p",    settingsProvider.GetMainAmperage(), 3, 35,    ports1_6, "P1", Type3PN.P3)},
            //    { "Introductory3pn",  new Introductory(  "Introductory3pn",   settingsProvider.GetMainAmperage(), 4, 35,    ports1_8, "P1", Type3PN.P3_N)},
            //    { "SPD3",             new Component(     "SPD3",                                             100, 4, 65,    ports2x2i)},
            //    { "DinRailMeter3p",   new Component(     "DinRailMeter3p",                                   63,  6, 145,   ports1_8 )},
            //    { "RCDFire",          new RCDFire  (     "RCDFire",                                          63,  4, 75,    ports1_8 )},
            //    { "VoltageRelay1",    new Component(     "VoltageRelay1",                                    16,  2, 40,    ports017 )},
            //    { "VoltageRelay2",    new Component(     "VoltageRelay2",                                    16,  2, 40,    ports237 )},
            //    { "VoltageRelay3",    new Component(     "VoltageRelay3",                                    16,  2, 40,    ports457 )},
            //    { "VoltageRelay",     new Component(     "VoltageRelay",                                     16,  2, 60,    ports1_7 )},
            //    { "DinRailSocket",    new Component(     "DinRailSocket",                                    16,  2, 22              )},
            //    { "ModularContactor", new Contactor(     "ModularContactor",                                 100, 4, 25              )},
            //    { "CrossBlock",       new Component(     "CrossBlock",                                       100, 4, 25,    ports1_8 )},
            //};


*/