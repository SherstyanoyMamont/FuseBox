using NUnit.Framework;
using System.Collections.Generic;
using FuseBox;
using FuseBox.App.Models.BaseAbstract;
using System.Security.Cryptography.X509Certificates;



//namespace FuseBox.Tests
//{
//    [TestFixture]
//    public class DistributeLogicTest
//    {
//        /*
//         * Все автотесты работают с входными данными, которые инициируются в static методах, которые собственно возвращают тестовые кейсы:
//         *  - Можно добавлять новые тест кейсы через добавление новой строки yield return;
//         *  - Нельзя менять последовательность ввода переменных, сокращать их;
//         * 
//         *  Специфика формата тестовых кейсов не позволяет протестировать все методы в проекте за раз,
//         *  поэтому при необходимости протестировать конкретный метод необходимо найти его в коде, 
//         *  нажать ПКМ и запустить тест. Протестируеться в итоге только он, остальные не запустятся
//         *  (иногда бывают исключения, до конца не понятно почему)
//         *  
//         *  Перед тестовыми методами есть место с комментариями по поводу 
//         *  ошибок/ньюансов ввода/логики работы/предложений по улучшений тестируемых методов
//         */

//        public static IEnumerable<object[]> GetAllConsumers()
//        {

//            yield return new object[]
//            {
//                "Case 1",
//                new Project
//                (
//                    new InitialSettings(1, 16),
//                    new FloorGrouping(true, false),
//                    new GlobalGrouping(0, 0, 1),
//                    new List<Floor>
//                    {
//                        new Floor
//                        (
//                            new List<Room>
//                            {
//                                new Room( new List<Consumer> { new Consumer("Air Conditioner", 16) } )
//                            }
//                        )
//                    }
//                )
//            };
//            yield return new object[]
//            {
//                "Case 2",
//                new Project
//                (
//                    new InitialSettings(3, 16),
//                    new FloorGrouping(true, false),
//                    new GlobalGrouping(0, 1, 0),
//                    new List<Floor>
//                    {
//                        new Floor
//                        (
//                            new List<Room>
//                            {
//                                new Room( new List<Consumer> { new Consumer("Lighting", 16) } )
//                            }
//                        )
//                    }
//                )
//            };
//            yield return new object[]
//            {
//                "Case 3",
//                new Project
//                (
//                    new InitialSettings(3, 16),
//                    new FloorGrouping(true, false),
//                    new GlobalGrouping(1, 0, 0),
//                    new List<Floor>
//                    {
//                        new Floor
//                        (
//                            new List<Room>
//                            {
//                                new Room( new List<Consumer> { new Consumer("Socket", 16) } )
//                            }
//                        )
//                    }
//                )
//            };
//            yield return new object[]
//            {
//                "Case 4",
//                new Project
//                (
//                    new InitialSettings(1, 16),
//                    new FloorGrouping(true, false),
//                    new GlobalGrouping(1, 1, 1),
//                    new List<Floor>
//                    {
//                        new Floor
//                        (
//                            new List<Room>
//                            {
//                                new Room
//                                (
//                                    new List<Consumer>
//                                    {
//                                        new Consumer("Socket", 16),
//                                        new Consumer("Lighting", 16),
//                                        new Consumer("Air Conditioner", 10)
//                                    }
//                                )
//                            }
//                        )
//                    }
//                )
//            };
//            yield return new object[]
//            {
//                "Case 5",
//                new Project
//                (
//                    new InitialSettings(3, 16),
//                    new FloorGrouping(true, false),
//                    new GlobalGrouping(2, 2, 0),
//                    new List<Floor>
//                    {
//                        new Floor
//                        (
//                            new List<Room>
//                            {
//                                new Room
//                                (
//                                    new List<Consumer>
//                                    {
//                                        new Consumer("Socket", 16),
//                                        new Consumer("Socket", 16),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Heating floor", 15),
//                                        new Consumer("Refrigerator", 15),
//                                        new Consumer("Oven", 20)
//                                    }
//                                )
//                            }
//                        )
//                    }
//                )
//            };
//            yield return new object[]
//            {
//                "Case 6",
//                new Project
//                (
//                    new InitialSettings(3, 16),
//                    new FloorGrouping(true, false),
//                    new GlobalGrouping(3, 6, 3),
//                    new List<Floor>
//                    {
//                        new Floor
//                        (
//                            new List<Room>
//                            {
//                                new Room
//                                (
//                                    new List<Consumer>
//                                    {
//                                        new Consumer("Socket", 16),
//                                        new Consumer("Socket", 16),
//                                        new Consumer("Socket", 16),
//                                        new Consumer("Socket", 16),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Lighting", 10),
//                                        new Consumer("Air Conditioner", 15),
//                                        new Consumer("Air Conditioner", 15),
//                                        new Consumer("Air Conditioner", 15),
//                                        new Consumer("Refrigerator", 15),
//                                        new Consumer("Oven", 20)
//                                    }
//                                )
//                            }
//                        )
//                    }
//                )
//            };
//        }

//        /* 1. Нужно добавить проверку на перегруз автомата потребителями по амперажу
//           2. Нужно добавить возможность отключить GlobalGrouping (разделять автоматы по помещениям, а не потребителям)
//           3. OK
//        */
//        [TestCaseSource(nameof(GetAllConsumers))]
//        public void DistributeOfConsumersTest(string caseName, Project testProject)   //Тестировка глобальной групировки 
//        {
//            // Создаем все необходимые объекты для работы теста
//            //Project testProject = new Project();
//            //var avFuses = new List<Fuse>();

//            List<RCD> uzos = new List<RCD>();
//            DistributionService distributionService = new DistributionService(testProject, uzos);

//            // Запускаем тестируемый метод, который должен заполнить список avFuses
//            distributionService.DistributeOfConsumers();

//            // Получаем эталонные данные для сравнения с результатами работы тестируемого метода
//            int countOfFuses = 0;
//            var consumers = testProject.Floors[0].Rooms[0].Consumer;
//            for (int i = 0; i < consumers.Count(); i++)
//            {
//                if (consumers[i].Name == "Socket" || consumers[i].Name == "Lighting" || consumers[i].Name == "Air Conditioner")
//                    continue;
//                countOfFuses++;
//            }
//            countOfFuses += testProject.GlobalGrouping.Sockets + testProject.GlobalGrouping.Lighting + testProject.GlobalGrouping.Conditioners;

//            // Собственно, сама проверка
//            Assert.That(distributionService.AVFuses.Count, Is.EqualTo(countOfFuses));
//        }

//        // Вспомагательный метод для тестов
//        public double GetTotalPower(List<BaseElectrical> consumers)
//        {
//            double totalPower = 0;
//            foreach (var consumer in consumers)
//                totalPower += consumer.Amper;
//            return totalPower;
//        }


//        /*
//         * 1. Когда создаются дополнительные узо, нужно проверить его параметр ампеража (нужны ли всегда УЗО на 63 ампера)
//         * 2. Мне кажеться, что взяли слишком большой запас мощности по УЗО (смотри переменные RCD64A и прочие)
//         * 3. Проверить возможнсть разбивать автоматы на несколько (Может быть на один автомат идти 32А, и это не один потребитель)
//         * Ok
//         */
//        [TestCaseSource(nameof(GetAllConsumers))]
//        public void DistributeRCDFromLoadTest(string caseName, Project testProject)
//        {
//            // Создаем необходимые объекты для работы теста 
//            //Project testPoject = new Project();
//            //var avFuses = new List<Fuse>();
//            var consumers = testProject.Floors[0].Rooms[0].Consumer;
//            List<RCD> uzos = new List<RCD>();
//            DistributionService distributionService = new DistributionService(testProject, uzos);

//            // Переписал пару констант с DistributionService
//            double RCD64A = 32.00;
//            double RCDPerPhases = 3.00;

//            double tAmper = GetTotalPower(consumers.Cast<BaseElectrical>().ToList());


//            // Запускаем тестируемый метод. Он работает только в связке с DistributeOfConsumers, поэтому запускаем и его
//            distributionService.DistributeOfConsumers();
//            distributionService.DistributeRCDFromLoad();        //tAmper, uzos, avFuses, (int)phaseCount


//            // Получаем данные для референса

//            // Получаем эталонное необходимое кол-во созданых УЗО для первого сравнения          
//            var uzoCount = 0;
//            var phaseCount = testProject.InitialSettings.PhasesCount;
//            if (tAmper <= RCD64A) uzoCount = 1;
//            else
//            {
//                uzoCount = (int)Math.Ceiling(tAmper / RCD64A);
//                switch (phaseCount)
//                {
//                    case 1:
//                        {

//                            if (uzoCount < (int)Math.Ceiling(distributionService.AVFuses.Count / RCD.LimitOfConnectedFuses))
//                                uzoCount = (int)Math.Ceiling(distributionService.AVFuses.Count / RCD.LimitOfConnectedFuses);
//                            break;
//                        }
//                    case 3:
//                        {
//                            if (uzoCount != 3)
//                            {
//                                uzoCount = (int)Math.Ceiling(uzoCount / RCDPerPhases) * (int)RCDPerPhases;
//                            }
//                            else uzoCount = 3;
//                            break;
//                        }
//                }
//            }

//            // Проверить равномерность распределения мощности между УЗО (Максимальное значение дельты - мощность самого нагруженого автомата)
//            double maxLoadFuse = 0;
//            foreach (var fuse in distributionService.AVFuses)  //  поиск ампеража самого нагруженого автомата
//            {
//                if (GetTotalPower(fuse.Electricals) > maxLoadFuse)
//                {
//                    maxLoadFuse = 0;
//                    foreach (var electrical in fuse.Electricals)
//                    {
//                        maxLoadFuse += electrical.Amper;
//                    }
//                }
//            }

//            var uzoLoads = distributionService.uzos.Select(uzo =>
//                uzo.Electricals
//                .OfType<Fuse>()
//                .Sum(fuse => fuse.Electricals
//                .OfType<Consumer>()
//                .Sum(consumer => consumer.Amper)))
//                .ToList();
//            double maxLoad = uzoLoads.Max();
//            double minLoad = uzoLoads.Min();

//            // Сама проверка
//            Assert.AreEqual(distributionService.uzos.Count, uzoCount, $"в {caseName} неверное кол-во УЗО");
//            Assert.LessOrEqual((int)(maxLoad - minLoad), maxLoadFuse, $"в {caseName} неравномерная нагрузка!");
//        }
//    }


//}
