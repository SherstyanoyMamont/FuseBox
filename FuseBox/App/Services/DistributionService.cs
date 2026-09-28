using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using System.Collections.Generic;
using System.Xml.Linq;

namespace FuseBox
{
    public class DistributionService : IDistributionService
    {
        private readonly IProjectSettings settingsProvider;
        private readonly IProjectGrouping projectGrouping;
        private readonly IConsumerProvider consumerProvider;
        private readonly IComponentFactory componentFactory;

        public List<Consumer> Lightings = new();
        public List<Consumer> Socket = new();
        public List<Consumer> AirConditioner = new();
        public List<Consumer> HeatedFloor = new();
        public List<Fuse> AVFuses = new();
        public List<RCD> uzos = new();

        //public Project project;
        public double countOfRCD;

        // Делаем запас в два раза
        public double RCD16A = 8.00;
        public double RCD32B = 16.00;
        public double RCD64A = 32.00;
        public double AVPerRCD = 6.00;
        public double RCDPerPhases = 3.00;

        public DistributionService(IProjectSettings settingsProvider, IProjectGrouping projectGrouping, IConsumerProvider consumerProvider, IComponentFactory componentFactory)
        {
            this.settingsProvider = settingsProvider;
            this.projectGrouping = projectGrouping;
            this.consumerProvider = consumerProvider;
            this.componentFactory = componentFactory;
        }

        public List<RCD> GetDistributedRCDModules()
        {
            return uzos;
        }

        // Логика распределения модулей по порядку
        public void DistributeOfConsumers()
        {
            AVFuses.Clear();

            var allConsumers = consumerProvider.GetAllConsumers();

            string NormalizeName(string name)
            {
                return name.Trim().ToLowerInvariant();
            }

            bool IsGroupedCategory(Consumer consumer)
            {
                string name = NormalizeName(consumer.Name);

                return name == "lighting" ||
                       name == "socket" ||
                       name == "sockets" ||
                       name == "air conditioner" ||
                       name == "heated floor";
            }

            bool NeedsDedicatedBreaker(Consumer consumer)
            {
                return !IsGroupedCategory(consumer) ||
                       consumer.BreakerAmperage != 16 ||
                       consumer.RcdMilliAmps != 30;
            }

            // Приборы и потребители с индивидуальными настройками:
            // один потребитель — один автомат.
            foreach (var consumer in allConsumers.Where(NeedsDedicatedBreaker))
            {
                AVFuses.Add(
                    componentFactory.GetAVModule(
                        new List<Consumer> { consumer }));
            }

            var shared = allConsumers
                .Where(c => !NeedsDedicatedBreaker(c))
                .ToList();

            void AddCategory(
                int grouping,
                string category,
                params string[] names)
            {
                var consumers = shared
                    .Where(c => names.Contains(NormalizeName(c.Name)))
                    .ToList();

                if (consumers.Count > 0)
                {
                    AutomatPerCons(grouping, consumers, category);
                }
            }

            AddCategory(
                projectGrouping.GetLightingsGrouping(),
                "Lighting",
                "lighting");

            AddCategory(
                projectGrouping.GetSocketsGrouping(),
                "Sockets",
                "socket",
                "sockets");

            AddCategory(
                projectGrouping.GetConditionersGrouping(),
                "Air Conditioner",
                "air conditioner");

            AddCategory(
                1,
                "Heated Floor",
                "heated floor");
        }

        public void AutomatPerCons(
            int groupingParam,
            List<Consumer> consumers,
            string? name)
        {
            if (consumers.Count == 0)
                return;

            if (groupingParam < 0)
                throw new ArgumentException("Invalid grouping parameter.");

            if (groupingParam == 0)
            {
                // Group by room first, then automatically split an overloaded
                // room into as many circuit breakers as necessary.
                if (consumers.Any(c => c.Room == null))
                {
                    throw new ArgumentException(
                        "A consumer must be assigned to a room before grouping.");
                }

                foreach (var roomGroup in consumers.GroupBy(c => c.Room))
                {
                    AddAutoSizedBreakerGroups(
                        roomGroup.ToList(),
                        minimumGroupCount: 1,
                        categoryName: name);
                }

                return;
            }

            // A positive setting is now the minimum desired number of groups.
            // If that is not enough for the selected breaker rating, the backend
            // automatically increases the number of groups until every group fits.
            AddAutoSizedBreakerGroups(
                consumers,
                minimumGroupCount: groupingParam,
                categoryName: name);
        }

        private void AddAutoSizedBreakerGroups(
            List<Consumer> consumers,
            int minimumGroupCount,
            string? categoryName)
        {
            if (consumers.Count == 0)
                return;

            int nominal = consumers[0].BreakerAmperage;
            int sensitivity = consumers[0].RcdMilliAmps;

            if (consumers.Any(c =>
                c.BreakerAmperage != nominal ||
                c.RcdMilliAmps != sensitivity))
            {
                throw new ArgumentException(
                    $"{categoryName ?? "The consumer group"} contains consumers " +
                    "with different protection settings.");
            }

            var impossibleConsumer = consumers.FirstOrDefault(c => c.Amper > nominal);

            if (impossibleConsumer != null)
            {
                throw new ArgumentException(
                    $"{impossibleConsumer.Name} draws {impossibleConsumer.Amper:F2} A, " +
                    $"which exceeds its C{nominal} circuit breaker.");
            }

            double totalCurrent = consumers.Sum(c => c.Amper);

            // Start with the user's requested minimum, but skip directly to the
            // theoretical minimum when the total current already requires more.
            int requiredByTotal = nominal > 0
                ? (int)Math.Ceiling(totalCurrent / nominal)
                : consumers.Count;

            int groupCount = Math.Clamp(
                Math.Max(Math.Max(minimumGroupCount, 1), requiredByTotal),
                1,
                consumers.Count);

            while (groupCount <= consumers.Count)
            {
                var buckets = Enumerable.Range(0, groupCount)
                    .Select(_ => new List<Consumer>())
                    .ToList();

                bool packed = true;

                // Largest loads first. Prefer the currently least-loaded bucket
                // that can still accept the consumer without exceeding the breaker.
                foreach (var consumer in consumers.OrderByDescending(c => c.Amper))
                {
                    var bucket = buckets
                        .Select(group => new
                        {
                            Group = group,
                            Load = group.Sum(c => c.Amper)
                        })
                        .Where(candidate =>
                            candidate.Load + consumer.Amper <= nominal + 0.000001)
                        .OrderBy(candidate => candidate.Load)
                        .ThenBy(candidate => candidate.Group.Count)
                        .Select(candidate => candidate.Group)
                        .FirstOrDefault();

                    if (bucket == null)
                    {
                        packed = false;
                        break;
                    }

                    bucket.Add(consumer);
                }

                if (packed)
                {
                    foreach (var bucket in buckets.Where(group => group.Count > 0))
                    {
                        AVFuses.Add(componentFactory.GetAVModule(bucket));
                    }

                    return;
                }

                groupCount++;
            }

            // In practice this can only be reached when the input data is invalid,
            // because one consumer per breaker must always fit after the check above.
            throw new InvalidOperationException(
                $"{categoryName ?? "The consumer group"} could not be distributed " +
                "across circuit breakers.");
        }
        public void DistributeRCDFromLoad()
        {
            uzos.Clear();

            if (AVFuses.Count == 0)
            {
                countOfRCD = 0;
                return;
            }

            int limit = (int)RCD.LimitOfConnectedFuses;

            if (limit <= 0)
                throw new InvalidOperationException("Invalid circuit-breaker limit for the RCD.");

            // На этом этапе сохраняем используемый в проекте номинал RCD 63 А.
            // Чувствительность 10/30 мА — отдельный параметр Capacity.
            const int rcdNominal = 63;

            foreach (var breaker in AVFuses.OrderByDescending(b => b.GetTotalLoad()))
            {
                if (breaker.Electricals.Count == 0)
                {
                    throw new InvalidOperationException(
                        "A circuit breaker without consumers was detected.");
                }

                int sensitivity = breaker.Electricals[0].RcdMilliAmps;

                if (breaker.Electricals.Any(c =>
                    c.RcdMilliAmps != sensitivity))
                {
                    throw new InvalidOperationException(
                        "Consumers on the same circuit breaker have different RCD requirements.");
                }

                double load = breaker.GetTotalLoad();

                if (!double.IsFinite(load) || load < 0 || load > rcdNominal)
                {
                    throw new ArgumentException(
                        "The circuit-breaker load is not supported by the current RCD model.");
                }

                var target = uzos
                    .Where(rcd =>
                        rcd.Capacity == sensitivity &&
                        rcd.Electricals.Count < limit &&
                        rcd.TotalLoad + load <= rcd.Amper)
                    .OrderBy(rcd => rcd.TotalLoad)
                    .FirstOrDefault();

                if (target == null)
                {
                    target = componentFactory.GetRCDModule(
                        rcdNominal,
                        new List<Fuse>());

                    target.Capacity = sensitivity;
                    target.TotalLoad = 0;

                    uzos.Add(target);
                }

                target.Electricals.Add(breaker);
                target.TotalLoad += load;

            }

            countOfRCD = uzos.Count;

            if (settingsProvider.GetPhasesCount() == 3)
            {
                DistributePerPhases();
            }
        }

        public void DistributePerPhases()
        {
            var phaseLoads = new double[3];

            foreach (var rcd in uzos.OrderByDescending(r => r.TotalLoad))
            {
                int phaseIndex = 0;

                if (settingsProvider.GetPhasesCount() == 3)
                {
                    phaseIndex = Array.IndexOf(
                        phaseLoads,
                        phaseLoads.Min());
                }

                var phasePort = rcd.Ports.FirstOrDefault(port =>
                    port.portOut == "Phase1" ||
                    port.portOut == "Phase2" ||
                    port.portOut == "Phase3");

                if (phasePort == null)
                {
                    throw new InvalidOperationException(
                        "The RCD does not have a phase port.");
                }

                phasePort.portOut = $"Phase{phaseIndex + 1}";

                phasePort.connectorColour = phaseIndex switch
                {
                    0 => "Red",
                    1 => "Orange",
                    _ => "Grey"
                };

                phaseLoads[phaseIndex] += rcd.TotalLoad;
            }
        }
    }
}

        /*public void Distribute()
        {
            int AVCount = AVFuses.Count;

            if (settingsProvider.GetPhasesCount() == 1)
            {
                if (countOfRCD < Math.Ceiling(AVCount / AVPerRCD))
                {
                    countOfRCD = Math.Ceiling(AVCount / AVPerRCD);
                }
                for (int i = 0; i < countOfRCD; i++)
                {
                    //uzos.Add(new RCD("RCD", 63, 2, 43, new List<Component>()));

                    uzos.Add(componentFactory.GetRCDModule(63, new List<Fuse>()));
                }

                while (uzos.Count < Math.Ceiling(AVCount / RCD.LimitOfConnectedFuses))        //&& uzos.Count < Math.Ceiling(AVCount / RCD.LimitOfConnectedFuses)
                {
                    uzos.Add(componentFactory.GetRCDModule(63, new List<Fuse>()));
                    countOfRCD++;
                }
            }
            else
            {
                // Если больше 3, округляем вверх до ближайшего кратного 3 
                if (countOfRCD > RCDPerPhases) // !!!
                    countOfRCD = Math.Ceiling(countOfRCD / RCDPerPhases) * RCDPerPhases;

                // Добавляем УЗО
                for (int i = 0; i < countOfRCD; i++)
                {
                    uzos.Add(componentFactory.GetRCDModule(63, new List<Fuse>()));
                }
            }
        }

        public void DistributePerPhases()
        {
            var phaseLoads = new double[3];

            foreach (var rcd in uzos.OrderByDescending(r => r.TotalLoad))
            {
                int phaseIndex = 0;

                if (settingsProvider.GetPhasesCount() == 3)
                {
                    phaseIndex = Array.IndexOf(
                        phaseLoads,
                        phaseLoads.Min());
                }

                var phasePort = rcd.Ports.FirstOrDefault(port =>
                    port.portOut == "Phase1" ||
                    port.portOut == "Phase2" ||
                    port.portOut == "Phase3");

                if (phasePort == null)
                {
                    throw new InvalidOperationException(
                        "The RCD does not have a phase port.");
                }

                phasePort.portOut = $"Phase{phaseIndex + 1}";
                phasePort.connectorColour = phaseIndex switch
                {
                    0 => "Red",
                    1 => "Orange",
                    _ => "Grey"
                };

                phaseLoads[phaseIndex] += rcd.TotalLoad;
            }
        }


        //public void DistributeFusesToRCDs()
        {
            List<RCD> filledRCDs = new List<RCD>();

            // Сортируем УЗО по их текущей нагрузке, чтобы равномерно распределять
            var uzoLoads = uzos.ToDictionary(uzo => uzo, uzo => 0); // Создаем словарь: УЗО -> текущая мощность (нагрузка)

            foreach (var breaker in AVFuses)
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

                //uzos.Add(breaker); // Добавляем УЗО в список, если оно еще не добавлено
                // 
                targetUzo.TotalLoad = targetUzo.TotalLoad + breaker.GetTotalLoad();
                targetUzo.Slots++;                                     // Увеличиваем количество слотов
                //targetUzo.OrderBreakersId();                         // Добавил функцию класса УЗО, который присвает новый id в порядке возрастания
                // Увеличиваем нагрузку для этого УЗО
                uzoLoads[targetUzo] += Convert.ToInt32(breakerLoad); /// !!!
            }
        }
        */
//Примерная мощность автомата С16 - 3.6 кВт.

// Примерная мощность УЗО      10А - 2.2 кВт.
// Примерная мощность УЗО      32А - 7 кВт.
// Примерная мощность УЗО      63А - 13,9 кВт. 

// Типы УЗО на 10-300 мА ()	10 А	16 А	25 А	32 А	40 А	64 А	80 А	100 А
// <30 мА – для защиты человека, 100> мА – для защиты зданий от пожаров

// 118 A

//double TotoalPower = project.TotalPower;
//decimal WireSection = Convert.ToDecimal(CalculateWireCrossSection(TotoalPower));





