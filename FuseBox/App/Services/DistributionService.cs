using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using ZstdSharp.Unsafe;

namespace FuseBox
{
    public class DistributionService
    {
        public List<Consumer> Lightings = new();
        public List<Consumer> Socket = new();
        public List<Consumer> AirConditioner = new();
        public List<Consumer> HeatedFloor = new();
        public List<Fuse> AVFuses = new();       
        public List<RCD> uzos;
        public List<Fuse> contactorFuses = new();
        public List<RCD> contactorRCDs;

        public Project project;
        public double countOfRCD;
        public double countOfContactorRCD;

        // Делаем запас в два раза
        public double RCD16A = 8.00;
        public double RCD32B = 16.00;
        public double RCD64A = 32.00;
        public double AVPerRCD = 6.00;
        public double RCDPerPhases = 3.00;

        public DistributionService(Project project, List<RCD> uzos, List<RCD> contactorRCDs)
        {
            this.project = project;
            this.uzos = uzos;
            this.contactorRCDs = contactorRCDs;
        }

        // Логика распределения модулей по порядку
        public void DistributeOfConsumers()
        {
            List<Consumer> AllConsumers = new();
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

            var consumerGroups = new Dictionary<string, List<Consumer>>
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
                    AVFuses.Add(new Fuse("AV", 16, 1, 10, new List<Consumer> { consumer }));
                }
            }
        }

        public void AutomatPerCons(int groupingParam, List<Consumer> list, string? name)
        {
            if (groupingParam == 0)
            {
                var buckets1 = new List<List<Consumer>>();

                // Создаем группы
                for (int i = 0; i < project.GetTotalNumberOfRooms(); i++)
                {
                    buckets1.Add(new List<Consumer>());
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
                var buckets = new List<List<Consumer>>(groupingParam);

                for (int i = 0; i < groupingParam; i++)
                {
                    buckets.Add(new List<Consumer>());
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
                DistributePerPhases(uzos, countOfRCD);
                CreateContactorFusesAndRCD();
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

                while (uzos.Count < Math.Ceiling(AVCount / RCD.LimitOfConnectedFuses))
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

        public void DistributePerPhases(List<RCD> rcdList, double rcdCount)      //  Распределить УЗО контактора по фазам
        {
            // Массив с нагрузкой на 3 фазы
            var phases = new int[3];

            for (int i = 0; i < rcdCount; i++)
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
                    rcdList[i].Ports[0].portOut = "Phase2";
                    rcdList[i].Ports[0].cableType.Сolour = "Orange";
                }
                else
                {
                    rcdList[i].Ports[0].portOut = "Phase3";
                    rcdList[i].Ports[0].cableType.Сolour = "Grey";
                }
                // Добавим нагрузку к фазе с минимальной нагрузкой
                phases[phaseIndex] = phases[phaseIndex] + Convert.ToInt32(rcdList[i].TotalLoad);
            }
        }   

        public void DistributeFusesToRCDs()
        {
            // Сортируем УЗО по их текущей нагрузке, чтобы равномерно распределять
            var uzoLoads = uzos.ToDictionary(uzo => uzo, uzo => 0); // Создаем словарь: УЗО -> текущая мощность (нагрузка)         

            foreach (var breaker in AVFuses)
            {
                // Вычисляем мощность автомата как сумму всех его потребителей
                double breakerLoad = breaker.Electricals.Sum(consumer => consumer.Amper);                                                              
                FindLeastLoadedRCD(uzoLoads, breaker, breakerLoad);               
            }
        }
        // Метод создаёт автоматы на основе потребителей из ContactorConsumers во fuseBox,
        // создаёт под них УЗО и распределяет автоматы, созданные под них
        public void CreateContactorFusesAndRCD()
        {
            foreach (var consumer in project.FuseBox.ContactorConsumers)                    // Создаём автоматы контактора
            {
                contactorFuses.Add(new Fuse("AV", 16, 1, 10, new List<Consumer> { consumer }));
            }

            CreateRCDForContactor();                                                       // Создаём УЗО контактора
            var contactorRCDsLoads = contactorRCDs.ToDictionary(rcd => rcd, rcd => 0);     // Создаем словарь: УЗО -> текущая мощность (нагрузка)

            foreach (var fuse in contactorFuses)                                           // Распределяем равновмерно автоматы по УЗО
            {
                double fuseLoad = fuse.Electricals.Sum(consumer => consumer.Amper);
                FindLeastLoadedRCD(contactorRCDsLoads, fuse, fuseLoad);
            }
            DistributePerPhases(contactorRCDs, countOfContactorRCD);                       // Распределяем равномерно УЗО по фазам
        }

        public void CreateRCDForContactor()
        {
            int contactorFuseCount = contactorFuses.Count();             // Считаем кол-во автоматов подключенные к контактором

            // Считаем сколько УЗО будет подключено к контактору
            countOfContactorRCD = (int)Math.Ceiling(contactorFuseCount / RCD.LimitOfConnectedFuses);

            // Создаём УЗО под контакторы
            for (int i = 0; i < countOfContactorRCD; i++)
            {
                contactorRCDs.Add(new RCD("RCD", 63, 2, 43, new List<Component>()));
            }
        }

        public void FindLeastLoadedRCD(Dictionary<RCD, int> uzoDic, Fuse breaker, double breakerLoad)
        {
            RCD targetUzo = uzoDic.OrderBy(uz => uz.Value).First().Key;

            // Удаляем УЗО из списка, если у него уже 5 выключателей
            if (targetUzo.Electricals.Count >= RCD.LimitOfConnectedFuses)   // Возможно баг - ранний переход на следующую итерацию цикла
            {
                uzoDic.Remove(targetUzo);
                // Ищем новый наименее нагруженное УЗО
                targetUzo = uzoDic.OrderBy(uz => uz.Value).First().Key;
            }

            // Добавляем автомат к выбранному УЗО
            targetUzo.Electricals.Add(breaker);
            targetUzo.TotalLoad = targetUzo.TotalLoad + breaker.GetTotalLoad();
            targetUzo.Slots++;     // Увеличиваем количество слотов

            // Увеличиваем нагрузку для этого УЗО
            uzoDic[targetUzo] += Convert.ToInt32(breakerLoad); /// !!!
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



