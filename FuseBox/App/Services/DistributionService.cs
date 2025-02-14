using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using System.Collections.Generic;

namespace FuseBox
{
    public class DistributionService()
    {
        public List<BaseElectrical> Lightings = new();
        public List<BaseElectrical> Socket = new();
        public List<BaseElectrical> AirConditioner = new();
        public List<BaseElectrical> HeatedFloor = new();

        // Логика распределения модулей по порядку
        public void DistributeOfConsumers(GlobalGrouping globalGrouping, List<BaseElectrical> AllConsumers, List<Fuse> AVFuses)
        {
            // Логика распределения потребителей
            int heatingPerAV = 1;
            var consumerGroups = new Dictionary<string, List<BaseElectrical>>
            {
                { "Lighting", Lightings },
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
                AutomatPerCons(globalGrouping.Lighting, AVFuses, Lightings);
            }
            if (AllConsumers.Any(e => e.Name.Equals("Socket", StringComparison.OrdinalIgnoreCase)))
            {
                AutomatPerCons(globalGrouping.Sockets, AVFuses, Socket);
            }
            if (AllConsumers.Any(e => e.Name.Equals("Air Conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                AutomatPerCons(globalGrouping.Conditioners, AVFuses, AirConditioner);
            }
            if (AllConsumers.Any(e => e.Name.Equals("Heated Floor", StringComparison.OrdinalIgnoreCase)))
            {
                AutomatPerCons(heatingPerAV, AVFuses, HeatedFloor);
            }
            foreach (var consumer in AllConsumers) // Добавляем автоматы без сортировки
            {
                if (consumer.Name != "Lighting" && consumer.Name != "Socket" && consumer.Name != "Air Conditioner" && consumer.Name != "Heated Floor")
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, new List<BaseElectrical> { consumer }));
                }
            }
        }
        public void AutomatPerCons(int groupingParam, List<Fuse> AVFuses, List<BaseElectrical> List)
        {
            for (int i = 0; i < groupingParam; i++)
            {
                var consumers = DistributeEvenly(List, groupingParam);         // Выбирает все потребители нужно типа
                AVFuses.Add(new Fuse("AV", 16, 1, 10, consumers[i]));          // И пихает его в автомат

            }
        }

        public void DistributeRCDFromLoad(double TAmper, List<RCD> uzos, List<Fuse> AVFuses, int PhasesCount)
        {
            // Делаем запас в два раза
            double RCD16A = 8.00;
            double RCD32B = 16.00;
            double RCD64A = 32.00;
            double AVPerRCD = 6.00;
            double RCDPerPhases = 3.00;

            int AVCount = AVFuses.Count;
            double countOfRCD = Math.Ceiling(TAmper / RCD64A);

            // Логика распределения УЗО от нагрузки
            if (TAmper <= RCD16A)
            {
                // Создаем УЗО
                uzos.Add(new RCD("RCD", 16, 2, 43, new List<BaseElectrical>(AVFuses)));
            }
            else if (TAmper > RCD16A && TAmper <= RCD32B)
            {
                uzos.Add(new RCD("RCD", 32, 2, 43, new List<BaseElectrical>(AVFuses)));
            }
            else if (TAmper > 16 && TAmper <= 32)
            {
                uzos.Add(new RCD("RCD", 63, 2, 43, new List<BaseElectrical>(AVFuses)));
            }
            else
            {
                if (PhasesCount == 1)
                {
                    Distribute1P(countOfRCD, RCDPerPhases, uzos, AVCount, AVPerRCD);
                }
                else 
                {
                    Distribute3P(countOfRCD, RCDPerPhases, uzos);
                }
                DistributeFusesToRCDs(AVFuses, uzos);

                DistributePerPhases(countOfRCD, uzos);
            }
        }

        public void Distribute1P(double countOfRCD, double RCDPerPhases, List<RCD> uzos, int AVCount, double AVPerRCD)
        {
            if (countOfRCD < Math.Ceiling(AVCount / AVPerRCD))
            {
                countOfRCD = Math.Ceiling(AVCount / AVPerRCD);
            }
            for (int i = 0; i < countOfRCD; i++)
            {
                uzos.Add(new RCD("RCD", 63, 2, 43, new List<BaseElectrical>()));
            }
            while (uzos.Count != Math.Ceiling(AVCount / RCD.LimitOfConnectedFuses))
            {
                uzos.Add(new RCD("RCD", 63, 2, 43, new List<BaseElectrical>()));
                countOfRCD++;
            }
        }

        public void Distribute3P(double countOfRCD, double RCDPerPhases, List<RCD> uzos)
        {

            // Если больше 3, округляем вверх до ближайшего кратного 3 
            if (countOfRCD > RCDPerPhases) // !!!
            {
                countOfRCD = Math.Ceiling(countOfRCD / RCDPerPhases) * RCDPerPhases;
            }
            // Добавляем УЗО
            for (int i = 0; i < countOfRCD; i++)
            {
                uzos.Add(new RCD("RCD", 63, 2, 43, new List<BaseElectrical>()));
            }
        }

        public void DistributePerPhases(double countOfRCD, List<RCD> uzos)
        {
            // Массив с нагрузкой на 3 фазы
            var phases = new int[3];

            for (int i = 0; i < countOfRCD; i++)
            {
                // Найдем фазу с наименьшей текущей нагрузкой
                int min = phases.Min();

                int phaseIndex = Array.IndexOf(phases, min);


                // Распределяем по фазам
                if (phaseIndex == 0)
                {
                    // Оставляем первую фазу
                }
                else if (phaseIndex == 1)
                {
                    uzos[i].Ports[0].portOut = PortOut.Phase2;
                    uzos[i].Ports[0].cableType.Colour = ConnectorColour.Orange;
                    uzos[i].Ports[0].cableType.colour1 = "Orange";
                }
                else
                {
                    uzos[i].Ports[0].portOut = PortOut.Phase3;
                    uzos[i].Ports[0].cableType.Colour = ConnectorColour.Grey;
                    uzos[i].Ports[0].cableType.colour1 = "Grey";
                }

                // Добавим нагрузку к фазе с минимальной нагрузкой
                phases[phaseIndex] = phases[phaseIndex] + Convert.ToInt32(uzos[i].TotalLoad);

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
                targetUzo.TotalLoad = targetUzo.TotalLoad + breaker.GetTotalLoad();
                targetUzo.Slots++;                                     // Увеличиваем количество слотов
                //targetUzo.OrderBreakersId();                         // Добавил функцию класса УЗО, который присвает новый id в порядке возрастания
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

    }
}

// Примерная мощность автомата С16 - 3.6 кВт.

// Примерная мощность УЗО      10А - 2.2 кВт.
// Примерная мощность УЗО      32А - 7 кВт.
// Примерная мощность УЗО      63А - 13,9 кВт. 

// Типы УЗО на 10-300 мА ()	10 А	16 А	25 А	32 А	40 А	64 А	80 А	100 А
// <30 мА – для защиты человека, 100> мА – для защиты зданий от пожаров

// 118 A







//double TotoalPower = project.TotalPower;
//decimal WireSection = Convert.ToDecimal(CalculateWireCrossSection(TotoalPower));

//Cable cablePhase = new Cable(ConnectorColour.Red, Convert.ToDecimal(WireSection));
//Cable cableZero = new Cable(ConnectorColour.Blue, Convert.ToDecimal(WireSection));

////ports.Add(PhaseInRed = new Port(PortIn.Phase1, new Cable(ConnectorColour.Red, WireSection)));

//Port PhaseInRed     = new Port(PortIn. Phase1, new Cable (ConnectorColour.Red   , WireSection ));
//Port PhaseInOrange  = new Port(PortIn. Phase2, new Cable (ConnectorColour.Orange, WireSection ));
//Port PhaseInGrey    = new Port(PortIn. Phase3, new Cable (ConnectorColour.Grey  , WireSection ));
//Port ZeroInput      = new Port(PortIn. Zero,   new Cable (ConnectorColour.Blue  , WireSection ));
//Port PhaseOutRed    = new Port(PortOut.Phase1, new Cable (ConnectorColour.Red   , WireSection ));
//Port PhaseOutOrange = new Port(PortOut.Phase2, new Cable (ConnectorColour.Orange, WireSection ));
//Port PhaseOutGrey   = new Port(PortOut.Phase3, new Cable (ConnectorColour.Grey  , WireSection ));
//Port ZeroOut        = new Port(PortOut.Zero,   new Cable (ConnectorColour.Blue  , WireSection ));

//List<Port> Set4x4  = new List<Port>() { PhaseInRed, PhaseInOrange, PhaseInGrey, ZeroInput, PhaseOutRed, PhaseOutOrange, PhaseOutGrey, ZeroOut };
//List<Port> Set4In  = new List<Port>() { PhaseInRed, PhaseInOrange, PhaseInGrey, ZeroInput };
//List<Port> Set3x3  = new List<Port>() { PhaseInRed, PhaseInOrange, PhaseInGrey, ZeroInput, PhaseOutRed, PhaseOutOrange, PhaseOutGrey, ZeroOut };
//List<Port> VRelay1 = new List<Port>() { PhaseInRed, ZeroInput, PhaseOutRed };
//List<Port> VRelay2 = new List<Port>() { PhaseInOrange, ZeroInput, PhaseOutOrange };
//List<Port> VRelay3 = new List<Port>() { PhaseInGrey, ZeroInput, PhaseOutGrey };
//List<Port> VRelay4 = new List<Port>() { PhaseInRed, PhaseInOrange, PhaseInGrey, ZeroInput, PhaseOutRed, PhaseOutOrange, PhaseOutGrey };
//List<Port> Empty   = new List<Port>();