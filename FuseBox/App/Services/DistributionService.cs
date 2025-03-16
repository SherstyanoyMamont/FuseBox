using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using System.Collections.Generic;
using System.Xml.Linq;

namespace FuseBox
{
    public class DistributionService
    {
        public List<Component> Lightings = new();
        public List<Component> Socket = new();
        public List<Component> AirConditioner = new();
        public List<Component> HeatedFloor = new();
        public List<Fuse> AVFuses = new();
        public List<RCD> uzos;

        public Project project;
        public double countOfRCD;

        // Делаем запас в два раза
        public double RCD16A = 8.00;
        public double RCD32B = 16.00;
        public double RCD64A = 32.00;
        public double AVPerRCD = 6.00;
        public double RCDPerPhases = 3.00;

        public DistributionService(Project project, List<RCD> uzos)
        {
            this.project = project;
            this.uzos = uzos;
        }

        // Логика распределения модулей по порядку
        public void DistributeOfConsumers()
        {
            List<Component> AllConsumers = new();
            // Логика распределения потребителей
            int heatingPerAV = 1;
            GlobalGrouping globalGrouping = project.GlobalGrouping;

            // Собираем все потребители в один список
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

            var consumerGroups = new Dictionary<string, List<Component>>
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
                AutomatPerCons(globalGrouping.Lighting, Lightings, "Lighting");
            }
            if (AllConsumers.Any(e => e.Name.Equals("Socket", StringComparison.OrdinalIgnoreCase)))
            {
                AutomatPerCons(globalGrouping.Sockets, Socket, "Socket");
            }
            if (AllConsumers.Any(e => e.Name.Equals("Air Conditioner", StringComparison.OrdinalIgnoreCase)))
            {
                AutomatPerCons(globalGrouping.Conditioners, AirConditioner, "Air Conditioner");
            }
            if (AllConsumers.Any(e => e.Name.Equals("Heated Floor", StringComparison.OrdinalIgnoreCase)))
            {
                AutomatPerCons(heatingPerAV, HeatedFloor, "Heated Floor");
            }
            foreach (var consumer in AllConsumers) // Добавляем автоматы без сортировки
            {
                if (consumer.Name != "Lighting" && consumer.Name != "Socket" && consumer.Name != "Air Conditioner" && consumer.Name != "Heated Floor")
                {
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, new List<Component> { consumer }));
                }
            }
        }

        public void AutomatPerCons(int groupingParam, List<Component> list, string? name)
        {
            if (groupingParam == 0)
            {
                var buckets1 = new List<List<Component>>();

                // Создаем группы
                for (int i = 0; i < project.GetTotalNumberOfRooms(); i++)
                {
                    buckets1.Add(new List<Component>());
                }

                // Распределяем потребителей по группам
                for (int i = 0; i < list.Count; i++)
                {
                    buckets1[i].Add(list[i]);
                }

                // Удаляем пустые группы
                buckets1.RemoveAll(innerList => innerList == null || innerList.Count == 0);


                // Создаём автоматы и добавляем в AVFuses
                for (int i = 0; i < buckets1.Count; i++)
                {
                    var consumers = buckets1[i];
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, consumers));
                }
            }
            else
            {
                var buckets = new List<List<Component>>(groupingParam);

                for (int i = 0; i < groupingParam; i++)
                {
                    buckets.Add(new List<Component>());
                }

                // Распределяем потребителей по группам
                for (int i = 0; i < list.Count; i++)
                {
                    buckets[i % groupingParam].Add(list[i]);
                }

                // Создаём автоматы и добавляем в AVFuses
                for (int i = 0; i < groupingParam; i++)
                {
                    var consumers = buckets[i];
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, consumers));
                }
            }

        }

        public void DistributeRCDFromLoad() 
        {
            double TAmper = project.CalculateTotalPower();

            countOfRCD = Math.Ceiling(TAmper / RCD64A);

            // Логика распределения УЗО от нагрузки
            if (TAmper <= RCD16A)
            {
                // Создаем УЗО
                uzos.Add(new RCD("RCD", 16, 2, 43, new List<Component>(AVFuses)));
            }
            else if (TAmper > RCD16A && TAmper <= RCD32B)
            {
                uzos.Add(new RCD("RCD", 32, 2, 43, new List<Component>(AVFuses)));
            }
            else if (TAmper > 16 && TAmper <= 32)
            {
                uzos.Add(new RCD("RCD", 63, 2, 43, new List<Component>(AVFuses)));
            }
            else
            {
                Distribute();
                DistributeFusesToRCDs();
                DistributePerPhases();
            }
        }

        public void Distribute()
        {
            int AVCount = AVFuses.Count;

            if (project.InitialSettings.PhasesCount == 1)
            {
                if (countOfRCD < Math.Ceiling(AVCount / AVPerRCD))
                {
                    countOfRCD = Math.Ceiling(AVCount / AVPerRCD);
                }
                for (int i = 0; i < countOfRCD; i++)
                {
                    uzos.Add(new RCD("RCD", 63, 2, 43, new List<Component>()));
                }

                while (uzos.Count < Math.Ceiling(AVCount / RCD.LimitOfConnectedFuses))        //&& uzos.Count < Math.Ceiling(AVCount / RCD.LimitOfConnectedFuses)
                {
                    uzos.Add(new RCD("RCD", 63, 2, 43, new List<Component>()));
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
                    uzos.Add(new RCD("RCD", 63, 2, 43, new List<Component>()));
                }
            }
        }

        public void DistributePerPhases()
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
                    uzos[i].Ports[0].portOut = "Phase2";
                    uzos[i].Ports[0].cableType.Сolour = "Orange";
                }
                else
                {
                    uzos[i].Ports[0].portOut = "Phase3";
                    uzos[i].Ports[0].cableType.Сolour = "Grey";
                }
                // Добавим нагрузку к фазе с минимальной нагрузкой
                phases[phaseIndex] = phases[phaseIndex] + Convert.ToInt32(uzos[i].TotalLoad);
            }
        }

        public void DistributeFusesToRCDs()
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
                targetUzo.TotalLoad = targetUzo.TotalLoad + breaker.GetTotalLoad();
                targetUzo.Slots++;                                     // Увеличиваем количество слотов
                //targetUzo.OrderBreakersId();                         // Добавил функцию класса УЗО, который присвает новый id в порядке возрастания
                // Увеличиваем нагрузку для этого УЗО
                uzoLoads[targetUzo] += Convert.ToInt32(breakerLoad); /// !!!
            }
        }
    }
}

//Примерная мощность автомата С16 - 3.6 кВт.

// Примерная мощность УЗО      10А - 2.2 кВт.
// Примерная мощность УЗО      32А - 7 кВт.
// Примерная мощность УЗО      63А - 13,9 кВт. 

// Типы УЗО на 10-300 мА ()	10 А	16 А	25 А	32 А	40 А	64 А	80 А	100 А
// <30 мА – для защиты человека, 100> мА – для защиты зданий от пожаров

// 118 A

//double TotoalPower = project.TotalPower;
//decimal WireSection = Convert.ToDecimal(CalculateWireCrossSection(TotoalPower));



