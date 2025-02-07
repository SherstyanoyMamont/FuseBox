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
        private int LastMainModuleId;
        // Создаем/Модифицируем объект проекта
        public Project GenerateConfiguration(Project input) // Метод возвращает объект ProjectConfiguration
        {
            //ValidateInitialSettings(input.InitialSettings); // Проверка первичных данных***

            // Создаем список всех потребителей и записываем их
            List<BaseElectrical> AllConsumers = CalculateAllConsumers(input.Floors);
            double TotoalPoverA = input.CalculateTotalPower();

            // Расчет параметров устройства электрощита
            FuseBox fuseBox;

            // Расчет сечения проводов для щитка
            

            if (input.InitialSettings.PhasesCount == 1) // Колличество фаз
            {
                fuseBox = ConfigureShield(input, input.FuseBox, AllConsumers);
            }
            else
            {
                fuseBox = ConfigureShield3(input, input.FuseBox, AllConsumers);
            }

            // Возвращаем новый модифицированный объект
            return new Project
            {
                InitialSettings = input.InitialSettings,
                GlobalGrouping = input.GlobalGrouping,
                FuseBox = fuseBox, // Возвращаем настроенный щит, а все остальное так же
                FloorGrouping = input.FloorGrouping,
                Floors = input.Floors,
                TotalPower = TotoalPoverA
            };
        }

        // Логика конфигурации устройств...
        private FuseBox ConfigureShield(Project project, FuseBox fuseBox, List<BaseElectrical> AllConsumers)             
        {

            List<Fuse> AVFuses = new List<Fuse>();
            List<Component> shieldModuleSet = new List<Component>();
            List<RCD> uzos = new List<RCD>();

            double TotoalPower = project.TotalPower;
            decimal WireSection = Convert.ToDecimal(CalculateWireCrossSection(TotoalPower));

            Cable cablePhase = new Cable(ConnectorColour.Red, Convert.ToDecimal(WireSection));
            Cable cableZero = new Cable(ConnectorColour.Blue, Convert.ToDecimal(WireSection));

            Port PhaseInRed = new Port(PortIn.Phase1,  new Cable (ConnectorColour.Red, WireSection));
            Port ZeroInput  = new Port(PortIn.Zero,    new Cable (ConnectorColour.Blue, WireSection ));
            Port PhaseOutRed   = new Port(PortOut.Phase1, new Cable (ConnectorColour.Red, WireSection ));
            Port ZeroOut    = new Port(PortOut.Zero,   new Cable (ConnectorColour.Blue, WireSection));

            List<Port> standartSet2x2 = new List<Port>() { PhaseInRed, ZeroInput, PhaseOutRed, ZeroOut };
            List<Port> justInput2     = new List<Port>() { PhaseInRed, ZeroInput};
            List<Port> empty          = new List<Port>();

            int standardSize = 2;

            if (fuseBox.MainBreaker)      { shieldModuleSet.Add(new Introductory("Introductory",Type3PN.P1,standartSet2x2,    standardSize, project.InitialSettings.MainAmperage, 35, "P1")); }
            if (fuseBox.SurgeProtection)  { shieldModuleSet.Add(new Component   ("SPD",              100,  justInput2,        standardSize, 65     )); }
            if (fuseBox.LoadSwitch2P)     { shieldModuleSet.Add(new Component   ("LoadSwitch",       63,   standartSet2x2,    standardSize, 35     )); }
            if (fuseBox.RailMeter)        { shieldModuleSet.Add(new Component   ("DinRailMeter",     63,   standartSet2x2,    6,            145    )); }
            if (fuseBox.FireUZO)          { shieldModuleSet.Add(new RCDFire     ("RCDFire",          63,   standartSet2x2,    standardSize, 75, 300)); }
            if (fuseBox.VoltageRelay)     { shieldModuleSet.Add(new Component   ("VoltageRelay",     16,   standartSet2x2,    standardSize, 40     )); }
            if (fuseBox.RailSocket)       { shieldModuleSet.Add(new Component   ("DinRailSocket",    16,   empty,             standardSize, 22     )); }
            if (fuseBox.NDiscLine)        { shieldModuleSet.Add(new RCD         ("NDiscLine",        25,   justInput2,        standardSize, 43, 30, new List<BaseElectrical>())); }
            if (fuseBox.LoadSwitch)       { shieldModuleSet.Add(new Component   ("LoadSwitch",       63,   standartSet2x2,    standardSize, 35     )); }
            if (fuseBox.ModularContactor) { shieldModuleSet.Add(new Contactor   ("ModularContactor", 100,                     4,            25, project.FuseBox.Contactor)); } // !!!
            if (fuseBox.CrossModule)      { shieldModuleSet.Add(new Component   ("CrossBlock",       100,  standartSet2x2,    4,            25     )); }

            //Cable cable = new Cable();
            //new Connector(ConnectorType.Input, cable);

            // Настройки автоматов для техники \\
            // Примерная мощность автомата С16 - 3.6 кВт.

            // Примерная мощность УЗО      10А - 2.2 кВт.
            // Примерная мощность УЗО      32А - 7 кВт.
            // Примерная мощность УЗО      63А - 13,9 кВт. 

            // Типы УЗО на 10-300 мА ()	10 А	16 А	25 А	32 А	40 А	64 А	80 А	100 А
            // <30 мА – для защиты человека, 100> мА – для защиты зданий от пожаров

            // 118 A

            //LastMainModuleId = Component._idCounter;   // Зафиксировали последний id главных модулей, дальше идут УЗО и автоматы

            // Логика распределения потребителей
            DistributeOfConsumers(project.GlobalGrouping, AllConsumers, AVFuses);

            // Логика распределения УЗО от нагрузки
            DistributeRCDFromLoad(project.CalculateTotalPower(), uzos, AVFuses);

            shieldModuleSet.AddRange(uzos);

            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                shieldModuleSet[i].Id = i + 1;
            }

            CreateConnections(shieldModuleSet, fuseBox.CableConnections);

            // Компоновка Щита по уровням...
            ShieldByLevel(project, project.FuseBox, shieldModuleSet);

            return fuseBox;
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        private FuseBox ConfigureShield3(Project project, FuseBox fuseBox, List<BaseElectrical> AllConsumers)
        {
            List<Fuse> AVFuses = new List<Fuse>();
            List<Component> shieldModuleSet = new List<Component>();
            List<RCD> uzos = new List<RCD>();

            double TotoalPower = project.TotalPower;
            decimal WireSection = Convert.ToDecimal(CalculateWireCrossSection(TotoalPower));

            Cable cablePhase = new Cable(ConnectorColour.Red, Convert.ToDecimal(WireSection));
            Cable cableZero = new Cable(ConnectorColour.Blue, Convert.ToDecimal(WireSection));

            Port PhaseInRed     = new Port(PortIn. Phase1,      new Cable (ConnectorColour.Red , WireSection ));
            Port PhaseInOrange  = new Port(PortIn. Phase2,      new Cable (ConnectorColour.Red , WireSection ));
            Port PhaseInGrey    = new Port(PortIn. Phase3,      new Cable (ConnectorColour.Red , WireSection ));
            Port ZeroInput      = new Port(PortIn. Zero,        new Cable (ConnectorColour.Blue, WireSection ));
            Port PhaseOutRed    = new Port(PortOut.Phase1,      new Cable (ConnectorColour.Red , WireSection ));
            Port PhaseOutOrange = new Port(PortOut.Phase2,      new Cable (ConnectorColour.Red , WireSection ));
            Port PhaseOutGrey   = new Port(PortOut.Phase3,      new Cable (ConnectorColour.Red , WireSection ));
            Port ZeroOut        = new Port(PortOut.Zero,        new Cable (ConnectorColour.Blue, WireSection ));

            List<Port> Set4x4  = new List<Port>() { PhaseInRed, PhaseInOrange, PhaseInGrey, ZeroInput, PhaseOutRed, PhaseOutOrange, PhaseOutGrey, ZeroOut };
            List<Port> Set4In  = new List<Port>() { PhaseInRed, PhaseInOrange, PhaseInGrey, ZeroInput };
            List<Port> Set3x3  = new List<Port>() { PhaseInRed, PhaseInOrange, PhaseInGrey, ZeroInput, PhaseOutRed, PhaseOutOrange, PhaseOutGrey, ZeroOut };
            List<Port> VRelay1 = new List<Port>() { PhaseInRed, ZeroInput, PhaseOutRed };
            List<Port> VRelay2 = new List<Port>() { PhaseInOrange, ZeroInput, PhaseOutOrange };
            List<Port> VRelay3 = new List<Port>() { PhaseInGrey, ZeroInput, PhaseOutGrey };
            List<Port> VRelay4 = new List<Port>() { PhaseInRed, PhaseInOrange, PhaseInGrey, ZeroInput, PhaseOutRed, PhaseOutOrange, PhaseOutGrey };
            List<Port> Empty   = new List<Port>();

            int standardSize = 4;

            // Настройки опцыонных автоматов \\

            if (fuseBox.MainBreaker)
            {
                // Если да, то добавляем 3 фазы + ноль
                if (project.FuseBox.Main3PN)
                {
                    shieldModuleSet.Add(new Introductory("Introductory 3P+N", Type3PN.P3_N, Set4x4, project.InitialSettings.MainAmperage, 2, 35, "P1"));
                }
                else
                {
                    shieldModuleSet.Add(new Introductory("Introductory 3P",   Type3PN.P3,   Set3x3, project.InitialSettings.MainAmperage, 2, 35, "P1"));
                }
            }
            if (fuseBox.SurgeProtection)  { shieldModuleSet.Add(new Component("SPD",              100, Set4In,    2, 65)); }
            if (fuseBox.RailMeter)        { shieldModuleSet.Add(new Component("DinRailMeter",     63,  Set4x4,    6, 145)); }
            if (fuseBox.FireUZO)          { shieldModuleSet.Add(new RCDFire  ("RCDFire",          63,  Set4x4,    2, 75, 300)); }
            if (fuseBox.VoltageRelay)
            {
                if (project.FuseBox.ThreePRelay)
                {
                    shieldModuleSet.Add(new Component("VoltageRelay", 16, VRelay4, 2, 60));

                } 
                else
                {
                    shieldModuleSet.Add(new Component("VoltageRelay", 16, VRelay1, 2, 40));
                    shieldModuleSet.Add(new Component("VoltageRelay", 16, VRelay2, 2, 40));
                    shieldModuleSet.Add(new Component("VoltageRelay", 16, VRelay3, 2, 40));
                }
            }
            if (fuseBox.RailSocket)       { shieldModuleSet.Add(new Component("DinRailSocket",    16,  Empty,     3, 22)); }
            if (fuseBox.ModularContactor) { shieldModuleSet.Add(new Contactor("ModularContactor", 100,            4, 25, project.FuseBox.Contactor)); } // !!!
            if (fuseBox.CrossModule)      { shieldModuleSet.Add(new Component("CrossBlock",       100, Set4x4,    4, 25)); }       // CrossModule? 4 slots?

            //LastMainModuleId = Component._idCounter;   // Зафиксировали последний id главных модулей, дальше идут УЗО и автоматы

            // Логика распределения потребителей
            DistributeOfConsumers(project.GlobalGrouping, AllConsumers, AVFuses);

            // Логика распределения УЗО от нагрузки
            DistributeRCDFromLoad(project.CalculateTotalPower(), uzos, AVFuses);

            shieldModuleSet.AddRange(uzos);

            for (int i = 0; i < shieldModuleSet.Count; i++)
            {
                shieldModuleSet[i].Id = i + 1; // ID начинается с 1
            }

            CreateConnections(shieldModuleSet, fuseBox.CableConnections);

            // Компоновка Щита по уровням...
            ShieldByLevel(project, project.FuseBox, shieldModuleSet);

            return fuseBox;
        }

        // Логика распределения модулей по порядку
        public void DistributeOfConsumers(GlobalGrouping globalGrouping, List<BaseElectrical> AllConsumers, List<Fuse> AVFuses)
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
                for (int i = 0; i < globalGrouping.Lighting; i++)
                {
                    var L = DistributeEvenly(Lighting, globalGrouping.Lighting); // Выбирает только весь свет
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, L[i]));                        // И пихает его в автомат
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("Socket", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < globalGrouping.Sockets; i++)
                {
                    var L = DistributeEvenly(Socket, globalGrouping.Sockets);
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, L[i]));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("Air Conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < globalGrouping.Conditioners; i++)
                {
                    var L = DistributeEvenly(AirConditioner, globalGrouping.Conditioners);
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, L[i]));
                }
            }
            if (AllConsumers.Any(e => e.Name.Equals("Heated Floor", StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < 1; i++)
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, HeatedFloor));
                }
            }
            foreach (var consumer in AllConsumers) // Добавляем автоматы без сортировки
            {
                if (consumer.Name != "Lighting" && consumer.Name != "Socket" && consumer.Name != "Air Conditioner" && consumer.Name != "Heated Floor")
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, new List<BaseElectrical> { consumer }));
                }
            }
        }
        public void DistributeRCDFromLoad(double TAmper, List<RCD> uzos, List<Fuse> AVFuses)
        {
            // Делаем запас в два раза
            double RCD16A = 8.00;
            double RCD32B = 16.00;
            double RCD64A = 32.00;
            double AVPerRCD = 6.00;

            int AVCount = AVFuses.Count;

            // Логика распределения УЗО от нагрузки
            if (TAmper <= RCD16A)
            {
                // Создаем УЗО
                uzos.Add(new RCD("RCD", 16, 2, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else if (TAmper > RCD16A && TAmper <= RCD32B)
            {
                uzos.Add(new RCD("RCD", 32, 2, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else if (TAmper > 16 && TAmper <= 32)
            {
                uzos.Add(new RCD("RCD", 63, 2, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else
            {
                double countOfRCD = Math.Ceiling(TAmper / RCD64A);

                if (countOfRCD < Math.Ceiling(AVCount / AVPerRCD))
                {
                    countOfRCD = Math.Ceiling(AVCount / AVPerRCD);
                }
                for (int i = 0; i < countOfRCD; i++)
                {
                    uzos.Add(new RCD("RCD", 63, 2, 43, 2, new List<BaseElectrical>()));
                }
                while (uzos.Count != Math.Ceiling(AVCount / RCD.LimitOfConnectedFuses))
                {
                    uzos.Add(new RCD("RCD", 63, 1, 43, 2, new List<BaseElectrical>()));
                    countOfRCD++;
                }
                DistributeFusesToRCDs(AVFuses, uzos);
            }
        }
        

        public void DistributeRCDFromLoad3P(double TAmper, List<RCD> uzos, List<Fuse> AVFuses)
        {
            // Делаем запас в два раза
            double RCD16A = 8.00;
            double RCD32B = 16.00;
            double RCD64A = 32.00;
            double RCDPerPhases = 3.00;

            // Логика распределения УЗО от нагрузки
            if (TAmper <= RCD16A)
            {
                // Создаем УЗО
                uzos.Add(new RCD("RCD", 16, 1, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else if (TAmper > RCD16A && TAmper <= RCD32B)
            {
                uzos.Add(new RCD("RCD", 32, 1, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else if (TAmper > RCD32B && TAmper <= RCD64A)
            {
                uzos.Add(new RCD("RCD", 63, 1, 43, 2, new List<BaseElectrical>(AVFuses)));
            }
            else
            {
                double countOfRCD = Math.Ceiling(TAmper / RCD64A);

                // Если больше 3, округляем вверх до ближайшего кратного 3 
                if (countOfRCD > RCDPerPhases) // !!!
                {
                    countOfRCD = Math.Ceiling(countOfRCD / RCDPerPhases) * RCDPerPhases;
                }
                for (int i = 0; i < countOfRCD; i++)
                {
                    uzos.Add(new RCD("RCD", 63, 1, 43, 2, new List<BaseElectrical>()));
                }
                DistributeFusesToRCDs(AVFuses, uzos);
            }
        }
        public void DistributeFusesToRCDs(List<Fuse> breakers, List<RCD> uzos)
        {
            List<RCD> filledRCDs = new List<RCD>();
            // Сортируем УЗО по их текущей нагрузке, чтобы равномерно распределять
            var uzoLoads = uzos.ToDictionary(uzo => uzo, uzo => 0); // Создаем словарь: УЗО -> текущая мощность (нагрузка)

            foreach (var breaker in breakers)
            {
                // Вычисляем мощность автомата как сумму всех его потребителей
                double breakerLoad = breaker.Electricals.Sum(consumer => consumer.Amper);

                // Находим УЗО с минимальной текущей нагрузкой
                var targetUzo = uzoLoads.OrderBy(uz => uz.Value).First().Key;

                // Удаляем УЗО из списка, если у него уже 5 выключателей
                if (targetUzo.Electricals.Count >= RCD.LimitOfConnectedFuses)
                {
                    filledRCDs.Add(targetUzo);
                    uzoLoads.Remove(targetUzo);                   
                    continue;
                }

                // Добавляем автомат к выбранному УЗО
                targetUzo.Electricals.Add(breaker);
                targetUzo.Slots++;                                     // Увеличиваем количество слотов
                //targetUzo.OrderBreakersId();                           // Добавил функцию класса УЗО, который присвает новый id в порядке возрастания
                // Увеличиваем нагрузку для этого УЗО
                uzoLoads[targetUzo] += Convert.ToInt32(breakerLoad); /// !!!
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
        public void ShieldByLevel(Project project, FuseBox fuseBox, List<Component> shieldModuleSet)
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

        // "Inter cable slot" - В этом пространстве можно прокладывать кабель
        // "Inter AV slot" - В этом пространстве нельзя прокладывать кабель

        //public void ShieldWithInterSlots(Project project)
        //{
        //    for (int i = 0; i <  project.FuseBox.Components.Count; i++)
        //    {
        //        for (int j = 0; j < project.FuseBox.Components[i].Count; j++)
        //        {
        //            if (project.FuseBox.Components[i][j] != project.FuseBox.Components[i][^1])
        //            {
        //                if (project.FuseBox.Components[i][j].Name == "RCD")
        //                {
        //                    var rcd = project.FuseBox.Components[i][j] as RCD;
        //                    for (int ii = 0; ii < rcd.Electricals.Count; ii++)
        //                    {
        //                        if (ii == 0)
        //                        {
        //                            rcd.Electricals.Insert(ii, new Space(0.5));
        //                            rcd.Slots += 0.5;
        //                        }
        //                        else if (rcd.Electricals[ii] == rcd.Electricals[^1])
        //                        {
        //                            rcd.Electricals.Insert(ii + 1, new Space(1)); // Между АВ и Пустым слотом 1 интерслот
        //                            rcd.Slots += 1;
        //                            ii++;
        //                        }
        //                        else
        //                        {
        //                            rcd.Electricals.Insert(ii + 1, new Space(0.5)); // Между АВ и АВ 0.5 интерслота
        //                            rcd.Slots += 0.5;
        //                            ii++;
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    project.FuseBox.Components[i].Insert(j + 1, new Space(1));    // Между модулями 1 интерслот
        //                    j++;
        //                }
                        
        //            }
        //        }

        //    }
        //}

        //public void CableConnections(List<List<BaseElectrical>> components)
        //{

        //}

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
                { 16, 80 }, { 25, 100 }, { 35, 125 }, { 50, 160 }
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
        public void CreateConnections(List<Component> components, List<Connection> сableConnections)
        {
            for (int i = 0; i < components.Count; i++)                                     // Берем каждый компонент
            {
                for (int port = 0; port < components[i].Ports.Count; port++)               // Берем каждый разьем компонента
                {
                    if (components[i].Ports[port].portOut != 0)                            // Но скипаем входы
                    {
                        for (var n = components[i].Id + 1; n < components.Count(); n++)    // Перебираем следующие компоненты по очереди
                        {
                            // Если у компонента есть такой же тип выхода, то есть и подходящий вход
                            if (components[n - 1].Ports.Any(e => e.portOut.ToString() == components[i].Ports[port].portOut.ToString()))
                            {
                                // Создаем соединение
                                AddConnection(сableConnections, components[i].Id, components[i].Ports[port], n);

                                // Берем следующий выходной разьем
                                break;
                            }
                            else
                            {
                                // Есть ли у него подходящий вход?
                                if (components[n].Ports.Any(e => e.PortIn.ToString() == components[i].Ports[port].portOut.ToString()))
                                {
                                    AddConnection(сableConnections, components[i].Id, components[i].Ports[port], n);

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

        //public bool IsConnectorIn(int id, List<Component> components)
        //{
        //    if (components[id].Connectors.Any(e => e.cableType == ))
        //    {

        //    }
        //}
        //public bool IsConnectorOut(int id, List<Component> components)
        //{

        //}
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
        // если это был последний элемент и остались слоты, добавляем пустое место
        if (remainingSlots > 0 && shieldModuleSet.Count == i + 1)
        {
            project.FuseBox.Components[levelIndex].Add(new Component("{empty space}", 0, remainingSlots, 0, 0));
        }
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

*/