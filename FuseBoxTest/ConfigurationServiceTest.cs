using FuseBox;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using NUnit.Framework.Interfaces;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace TestServices;

public class ConfigurationServiceTest
{
    /*
     * Все автотесты работают с входными данными, которые инициируются в static методах, которые собственно возвращают тестовые кейсы:
     *  - Можно добавлять новые тест кейсы через добавление новой строки yield return;
     *  - Нельзя менять последовательность ввода переменных, сокращать их;
     * 
     *  Специфика формата тестовых кейсов не позволяет протестировать все методы в проекте за раз,
     *  поэтому при необходимости протестировать конкретный метод необходимо найти его в коде, 
     *  нажать ПКМ и запустить тест. Протестируеться в итоге только он, остальные не запустятся
     *  (иногда бывают исключения, до конца не понятно почему)
     *  
     *  Перед тестовыми методами есть место с комментариями по поводу 
     *  ошибок/ньюансов ввода/логики работы/предложений по улучшений тестируемых методов
     */


    public static IEnumerable<double> AmperCases()
    {
        return new double[] { 10, 18, 24, 32, 63 };
        //return new double[] { 10, 18, 24, 32, 63 };
    }

    public static IEnumerable<object[]> ConfigureShieldCases()
    {
        yield return new object[]
        {   "Case 1",
            new FuseBox.FuseBox
            {
                MainBreaker = true,
                SurgeProtection = true,
                LoadSwitch2P = true,
                RailMeter = true,
                FireUZO = true,
                VoltageRelay = true,
                RailSocket = true,
                NDiscLine = true,
                LoadSwitch = true,
                ModularContactor = false, //true
                CrossModule = true
            },
            10, // Кол-во компонентов создатся при 1 фазе
            9,  // Кол-во компонентов создатся при 3 фазах
            1,  // Кол-во фаз
            18  // Кол-во соединений
        };
        yield return new object[]
        {
            "Case 2",
            new FuseBox.FuseBox
            {
                MainBreaker = false,
                SurgeProtection = false,
                LoadSwitch2P = false,
                RailMeter = false,
                FireUZO = false,
                VoltageRelay = false,
                RailSocket = false,
                NDiscLine = false,
                LoadSwitch = false,
                ModularContactor = false,
                CrossModule = false
            },
            0,
            0,
            1,
            0
        };
        yield return new object[]
        {
            "Case 3",
            new FuseBox.FuseBox
            {
                Main3PN = true, // 4
                SurgeProtection = true, //4
                LoadSwitch2P = true,    // 0
                ThreePRelay = true // 6
            },
            2,
            3,
            3,
            14
        };
        yield return new object[]
        {
            "Case 4",
            new FuseBox.FuseBox
            {
                MainBreaker = true,
                Main3PN = false,
                SurgeProtection = false,
                LoadSwitch2P = true,
                RailMeter = false,
                FireUZO = true,
                ThreePRelay = true,
                RailSocket = true,
                NDiscLine = false,
                LoadSwitch = true,
                ModularContactor = false,
                CrossModule = true
            },
            6,
            5,
            3,
            0,
        };
    }

    public static IEnumerable<object[]> ShieldModuleSetCases()
    {
        yield return new object[]
        {
            "Case 1",
            16,
            new List<Component>
            {
                new Introductory("Introductory", Type3PN.P1, 2),
                new Component("SPD", 2),//2
                new Component("LoadSwitch", 2),//4
                new Component("DinRailMeter",6),//4
                new RCDFire("RCDFire", 2),//4
                new Component("VoltageRelay", 2),//4
                new Component("DinRailSocket", 2),//0
                new RCD("NDiscLine", 25, 2, 43, new List<BaseElectrical>()),//2
                new Component("LoadSwitch", 2),//4
                new Contactor("ModularContactor", 4),//2
                new Component("CrossBlock", 4),//4
                new RCD("RCD", 25, 3, 0, new List<BaseElectrical>()),
                new RCD("RCD", 25, 5, 0, new List<BaseElectrical>()),
                new RCD("RCD", 25, 7, 0, new List<BaseElectrical>()),
            },
            45,
            3,
            1,

        };
        yield return new object[]
        {
            "Case 2",
            12,
            new List<Component>
            {
                new Introductory("Introductory", Type3PN.P1, 2),
                new Component("SPD", 2),
                new Component("DinRailMeter",6),
                new RCDFire("RCDFire", 2),
                new RCD("NDiscLine", 25, 2, 43, new List<BaseElectrical>()),
                new Component("LoadSwitch", 2),
                new Contactor("ModularContactor", 4),
                new Component("CrossBlock", 4),
                new RCD("RCD", 25, 4, 0, new List<BaseElectrical>()),
                new RCD("RCD", 25, 8, 0, new List<BaseElectrical>()),
                new RCD("RCD", 25, 5, 0, new List<BaseElectrical>()),

            },
            41,
            4,
            1,
        };
        yield return new object[]
        {
            "Case 3",
            16,                     // shield width
            new List<Component>
            {
                new Introductory("Introductory", Type3PN.P1, 2), //6
                new Component("SPD", 2),   //4
                new Component("LoadSwitch", 2),    //8
                new Component("DinRailMeter", 6),  //8
                new RCD("RCD", 25, 2, 0, new List<BaseElectrical>()), //0
            },
            14,                     // expected amount of slots
            1,                      // expected number of DINLevel
            3,                      // PhaseCount
        };

    }

    /*
     * Ок
     * 1. По задумке (и ГОСТ/ISO) у нас должна быть минимальная цепочка компонентов
     *    (например всегда должен присутствовать MainBreaker) - нужно исследовать этот метод для 
     *    дальнейшей валидности проверки данных;
     * 2. При генерации 3-фазного щитка отсутствует : LoadSwitch2P, LoadSwitch, NDiscLine
     * 3. На стадии ввода данных пользователем необходимо заблокировать добавление элементов, 
     *    если в списке уже есть элементы, которые противоречат созданому (MainBreaker и Main3PN, ThreePRelay и VoltageRelay)
     */
    [TestCaseSource(nameof(ConfigureShieldCases))]
    public void ConfigureShieldTest(string caseName, FuseBox.FuseBox fuseBox, int componentCountPhase1, int componentCountPhase3, int phaseCount, int connectionCount)
    {
        ConfigurationService config = new ConfigurationService();
        CreatePortsCombination(config);
        int amper = 0;

        if (phaseCount == 1)
        {
            // Запуск тестируемого метода
            config.ConfigureShield(fuseBox, amper);

            // Проверка
            var result = config.shieldModuleSet.Count;
            Assert.That(result, Is.EqualTo(componentCountPhase1));
        }
        else
        {
            // Запуск тестируемого метода
            config.ConfigureShield3(fuseBox, amper);

            // Проверка
            var result = config.shieldModuleSet.Count;
            Assert.That(result, Is.EqualTo(componentCountPhase3));
        }


    }

    /*
     * Нету обработки некорретных данных (0, отрицательное число тока и тд.)
     * Ok
     */
    [TestCaseSource(nameof(AmperCases))]
    public void CalculateWireCrossSection(double currentAmper)
    {
        ConfigurationService config = new ConfigurationService();
        double result,
               reference = 0;
        var copperWireTable = new Dictionary<double, double>
        {
            { 1.5, 18 }, { 2.5, 25 }, { 4, 32 }, { 6, 40 }, { 10, 63 },
            { 16, 80 }
        };
        foreach (var wire in copperWireTable) { if (currentAmper <= wire.Value) { reference = wire.Key; break; } }

        result = config.CalculateWireCrossSection(currentAmper);
        Assert.That(result, Is.EqualTo(reference));
    }

    /*
     * Ok
     */
    [TestCaseSource(nameof(ShieldModuleSetCases))]
    public void ShieldByLevelTest(string caseName, int shieldWidth, List<Component> shieldComponents, int slotsCount, int minDINLevelCount, int phaseCount)
    {
        // Создаем необходимые объекты для работы
        List<List<BaseElectrical>> resultList = new List<List<BaseElectrical>>();
        ConfigurationService config = new ConfigurationService();
        Project project = new Project();
        FuseBox.FuseBox fuseBox = new FuseBox.FuseBox();

        // Заполняем созданные объекты вводными данными
        config.shieldModuleSet = shieldComponents;
        project.InitialSettings.ShieldWidth = shieldWidth;

        // Запуск тестируемого метода
        config.ShieldByLevel(project, fuseBox);


        // Результат работы тестируемого метода записываем отдельно для проведения проверок
        resultList = fuseBox.Components;

        // Проверка #1
        if (minDINLevelCount > resultList.Count)
            Assert.Fail("Кол-во уровней щита меньше ожидаемого");

        // Блок кода проверяет количество занятых слотов на уровне.
        // Если какой то компонент помещался на уровне, но метод перенёс его на следующий - тест провалится 
        for (int i = 0; i < resultList.Count; i++)
        {
            Component firstComponentOnNewLine = new Component();
            if (i != resultList.Count - 1)
                firstComponentOnNewLine = resultList[i + 1][0] as Component;

            double slotsOnLvl = 0;
            for (int j = 0; j < resultList[i].Count; j++)
            {
                if (resultList[i][j].Name == "Empty Slot")
                {
                    EmptySlot emptySlot = resultList[i][j] as EmptySlot;
                    slotsOnLvl += emptySlot.Slots;
                }
                else
                {
                    Component thisComponent = resultList[i][j] as Component;
                    slotsOnLvl += thisComponent.Slots;
                }
            }
            if (shieldWidth - slotsOnLvl >= firstComponentOnNewLine.Slots && i != resultList.Count - 1)
                Assert.Fail($"Элемент {firstComponentOnNewLine.Name} должен быть на предидущем уровне щита "); 
        };
    }

    /* SPD, NDiscLine, Socket and Modular Cont doesn't have output ports
     * Метод работает некорректно для 3-фазного/3 однофазных реле напряжения, так как в них отсутствует выход на ноль
     */
    [TestCaseSource(nameof(ConfigureShieldCases))]
    public void CreateConnectionsTest(string caseName, FuseBox.FuseBox fuseBox, int componentCountPhase1, int componentCountPhase3, int phaseCount, int connectionCount)
    {
        ConfigurationService config = new ConfigurationService();
        Project project = new Project();
        int amper = 0;
        project.InitialSettings.PhasesCount = phaseCount;
        CreatePortsCombination(config);

        if (phaseCount == 1)
        {
            config.ConfigureShield(fuseBox, amper); // Заполняем shieldModuleSet компонентами и создаём порты
        }
        else
        {
            config.ConfigureShield3(fuseBox, amper);
        }
        // Создаём ID для модулей
        for (int i = 0; i < config.shieldModuleSet.Count; i++) { config.shieldModuleSet[i].Id = i + 1; }

        config.CreateConnections(fuseBox.CableConnections);

        Assert.That(fuseBox.CableConnections.Count, Is.EqualTo(connectionCount));

    }

    // Вспомогательный метод для создания портов компонентов config.shieldModuleSet
    public void CreatePortsCombination(ConfigurationService config)
    {
        var portData = new (PortInEnum? portIn, PortOutEnum? portOut, ConnectorColour colour)[]
        {
            (PortInEnum.Phase1, PortOutEnum.Phase1, ConnectorColour.Red),
            (PortInEnum.Phase2, PortOutEnum.Phase2, ConnectorColour.Orange),
            (PortInEnum.Phase3, PortOutEnum.Phase3, ConnectorColour.Grey),
            (PortInEnum.Zero,   PortOutEnum.Zero,   ConnectorColour.Blue)
        };
        foreach (var (portIn, portOut, colour) in portData)
        {
            if (portIn.HasValue)
                config.ports.Add(new Port(portIn.Value, new Cable(colour, 0)));

            if (portOut.HasValue)
                config.ports.Add(new Port(portOut.Value, new Cable(colour, 0)));
        }
    }

}
