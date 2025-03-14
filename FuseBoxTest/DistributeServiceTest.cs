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
//         * ��� ��������� �������� � �������� �������, ������� ������������ � static �������, ������� ���������� ���������� �������� �����:
//         *  - ����� ��������� ����� ���� ����� ����� ���������� ����� ������ yield return;
//         *  - ������ ������ ������������������ ����� ����������, ��������� ��;
//         * 
//         *  ��������� ������� �������� ������ �� ��������� �������������� ��� ������ � ������� �� ���,
//         *  ������� ��� ������������� �������������� ���������� ����� ���������� ����� ��� � ����, 
//         *  ������ ��� � ��������� ����. ��������������� � ����� ������ ��, ��������� �� ����������
//         *  (������ ������ ����������, �� ����� �� ������� ������)
//         *  
//         *  ����� ��������� �������� ���� ����� � ������������� �� ������ 
//         *  ������/�������� �����/������ ������/����������� �� ��������� ����������� �������
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

//        /* 1. ����� �������� �������� �� �������� �������� ������������� �� ��������
//           2. ����� �������� ����������� ��������� GlobalGrouping (��������� �������� �� ����������, � �� ������������)
//           3. OK
//        */
//        [TestCaseSource(nameof(GetAllConsumers))]
//        public void DistributeOfConsumersTest(string caseName, Project testProject)   //���������� ���������� ���������� 
//        {
//            // ������� ��� ����������� ������� ��� ������ �����
//            //Project testProject = new Project();
//            //var avFuses = new List<Fuse>();

//            List<RCD> uzos = new List<RCD>();
//            DistributionService distributionService = new DistributionService(testProject, uzos);

//            // ��������� ����������� �����, ������� ������ ��������� ������ avFuses
//            distributionService.DistributeOfConsumers();

//            // �������� ��������� ������ ��� ��������� � ������������ ������ ������������ ������
//            int countOfFuses = 0;
//            var consumers = testProject.Floors[0].Rooms[0].Consumer;
//            for (int i = 0; i < consumers.Count(); i++)
//            {
//                if (consumers[i].Name == "Socket" || consumers[i].Name == "Lighting" || consumers[i].Name == "Air Conditioner")
//                    continue;
//                countOfFuses++;
//            }
//            countOfFuses += testProject.GlobalGrouping.Sockets + testProject.GlobalGrouping.Lighting + testProject.GlobalGrouping.Conditioners;

//            // ����������, ���� ��������
//            Assert.That(distributionService.AVFuses.Count, Is.EqualTo(countOfFuses));
//        }

//        // ��������������� ����� ��� ������
//        public double GetTotalPower(List<BaseElectrical> consumers)
//        {
//            double totalPower = 0;
//            foreach (var consumer in consumers)
//                totalPower += consumer.Amper;
//            return totalPower;
//        }


//        /*
//         * 1. ����� ��������� �������������� ���, ����� ��������� ��� �������� �������� (����� �� ������ ��� �� 63 ������)
//         * 2. ��� ��������, ��� ����� ������� ������� ����� �������� �� ��� (������ ���������� RCD64A � ������)
//         * 3. ��������� ���������� ��������� �������� �� ��������� (����� ���� �� ���� ������� ���� 32�, � ��� �� ���� �����������)
//         * Ok
//         */
//        [TestCaseSource(nameof(GetAllConsumers))]
//        public void DistributeRCDFromLoadTest(string caseName, Project testProject)
//        {
//            // ������� ����������� ������� ��� ������ ����� 
//            //Project testPoject = new Project();
//            //var avFuses = new List<Fuse>();
//            var consumers = testProject.Floors[0].Rooms[0].Consumer;
//            List<RCD> uzos = new List<RCD>();
//            DistributionService distributionService = new DistributionService(testProject, uzos);

//            // ��������� ���� �������� � DistributionService
//            double RCD64A = 32.00;
//            double RCDPerPhases = 3.00;

//            double tAmper = GetTotalPower(consumers.Cast<BaseElectrical>().ToList());


//            // ��������� ����������� �����. �� �������� ������ � ������ � DistributeOfConsumers, ������� ��������� � ���
//            distributionService.DistributeOfConsumers();
//            distributionService.DistributeRCDFromLoad();        //tAmper, uzos, avFuses, (int)phaseCount


//            // �������� ������ ��� ���������

//            // �������� ��������� ����������� ���-�� �������� ��� ��� ������� ���������          
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

//            // ��������� ������������� ������������� �������� ����� ��� (������������ �������� ������ - �������� ������ ����������� ��������)
//            double maxLoadFuse = 0;
//            foreach (var fuse in distributionService.AVFuses)  //  ����� �������� ������ ����������� ��������
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

//            // ���� ��������
//            Assert.AreEqual(distributionService.uzos.Count, uzoCount, $"� {caseName} �������� ���-�� ���");
//            Assert.LessOrEqual((int)(maxLoad - minLoad), maxLoadFuse, $"� {caseName} ������������� ��������!");
//        }
//    }


//}
