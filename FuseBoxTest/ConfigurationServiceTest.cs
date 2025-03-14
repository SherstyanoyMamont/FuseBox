using FuseBox;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using NUnit.Framework.Interfaces;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace TestServices;

[TestFixture]
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
        {
            "Case 1",
            new Project
            (
                new FuseBox.FuseBox
                (
                    false,  // mainBraker
                    true,   // main3PN  false
                    true,   // surgeProtection 2
                    true,   // LoadSwitch2P  2
                    false,  // ModularContactor 
                    false,  // railMeter
                    false,  // fireUzo
                    true,   // VoltageRelay 2
                    false,  // ThreePRelay
                    false,  // RailSocket
                    true,  // NDiscLine 2
                    false,  // LoadSwitch
                    true   // crossModule 2
                ),
                new InitialSettings(1,16),
                new FloorGrouping(true,false)
            ),
            5, 0, 14,
            new List<Component>
            {
                new RCD("RCD", 25, 3, 0, new List<BaseElectrical>()), // 2
                new RCD("RCD", 25, 5, 0, new List<BaseElectrical>()), // 2
                new RCD("RCD", 25, 7, 0, new List<BaseElectrical>()), // last
            }
        };
        yield return new object[]
        {
            "Case 2",
            new Project
            (
                new FuseBox.FuseBox
                (
                    false,  // mainBraker
                    false,   // main3PN  
                    false,   // surgeProtection 
                    false,   // LoadSwitch2P  
                    false,  // ModularContactor 
                    false,  // railMeter
                    false,  // fireUzo
                    false,   // VoltageRelay 
                    false,  // ThreePRelay
                    false,  // RailSocket
                    false,  // NDiscLine 
                    false,  // LoadSwitch
                    false   // crossModule 
                ),
                new InitialSettings(1, 16),
                new FloorGrouping(true, false)
            ),
            0, 0, 0,
            new List<Component> { }
        };
        yield return new object[]
        {
            "Case 3",
            new Project
            (
                new FuseBox.FuseBox
                (
                    false,  // mainBraker
                    true,   // main3PN                  4
                    true,   // surgeProtection          6
                    true,   // LoadSwitch2P  false      0
                    false,  // ModularContactor
                    false,  // railMeter
                    false,  // fireUzo
                    true,   // VoltageRelay             last         
                    false,  // ThreePRelay
                    false,  // RailSocket
                    false,  // NDiscLine
                    false,  // LoadSwitch
                    false   // crossModule
                ),
                new InitialSettings(3,16),
                new FloorGrouping(true,false)
            ),
            0, 5, 10,
            new List<Component> { }
        };
        yield return new object[]
        {
            "Case 4",
            new Project
            (
                new FuseBox.FuseBox
                (
                    true,  // mainBraker                3
                    false,   // main3PN  false
                    false,   // surgeProtection
                    true,   // LoadSwitch2P  false      0
                    true,  // ModularContactor          0
                    true,  // railMeter                 4
                    false,  // fireUzo
                    false,   // VoltageRelay
                    true,  // ThreePRelay               4
                    false,  // RailSocket
                    true,  // NDiscLine  false          0
                    false,  // LoadSwitch
                    true   // crossModule               2
                ),
                new InitialSettings(3,16),
                new FloorGrouping(true,false)
            ),
            0, 5, 13,
            new List<Component> { new RCD("RCD", 25, 3, 0, new List<BaseElectrical>()) }  // last
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
    public void ConfigureShieldTest(string caseName, Project testProject, int componentCountPhase1, int componentCountPhase3, int connectionCount, List<Component> uzos)
    {
        ConfigurationService config = new ConfigurationService(testProject);
        //CreatePortsCombination(config);
        int amper = 0;

        config.ConfigureShield();

        var result = config.shieldModuleSet.Count;

        var phaseCount = testProject.InitialSettings.PhasesCount;

        if (phaseCount == 1) { Assert.That(result, Is.EqualTo(componentCountPhase1)); }
        else { Assert.That(result, Is.EqualTo(componentCountPhase3)); }
    }

    /*
     * Почему после рефакторинга это метод начал подбирать сечение кабеля только на входе щита?!
     * Раньше этот метод был гораздо более гибким и утилитарным
     * 
     * Не буду тестить пока не поговорю с Ваней
     * Нету обработки некорретных данных (0, отрицательное число тока и тд.)
     */
    //[TestCaseSource(nameof(AmperCases))]
    //public void CalculateWireCrossSection(double currentAmper)
    //{
    //    ConfigurationService config = new ConfigurationService();
    //    double result,
    //           reference = 0;

    //    config.CalculateWireCrossSection();

    //    var copperWireTable = new Dictionary<double, double>
    //    {
    //        { 1.5, 18 }, { 2.5, 25 }, { 4, 32 }, { 6, 40 }, { 10, 63 },
    //        { 16, 80 }
    //    };
    //    foreach (var wire in copperWireTable) { if (currentAmper <= wire.Value) { reference = wire.Key; break; } }

    //    Assert.That(result, Is.EqualTo(reference));
    //}

    ///*
    // * Ok
    // */
    [TestCaseSource(nameof(ConfigureShieldCases))]
    public void ShieldByLevelTest(string caseName, Project testProject, int componentCountPhase1, int componentCountPhase3, int connectionCount, List<Component> uzos)
    {
        // Создаем необходимые объекты для работы
        ConfigurationService config = new ConfigurationService(testProject);

        // Заполняем созданные объекты вводными данными
        config.ConfigureShield();
        config.shieldModuleSet.AddRange(uzos);

        // Запуск тестируемого метода
        config.ShieldByLevel();

        // Результат тестируемого метода - заполненый список fuseBox.Component
        var resultList = testProject.FuseBox.Components;
        // Узнаем эталонное количество созданых уровней щита
        int minDINLevelCount = config.shieldModuleSet.Count / testProject.InitialSettings.ShieldWidth;

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
            if (testProject.InitialSettings.ShieldWidth - slotsOnLvl >= firstComponentOnNewLine.Slots && i != resultList.Count - 1)
                Assert.Fail($"Элемент {firstComponentOnNewLine.Name} должен быть на предидущем уровне щита ");
        };
    }

    ///* SPD, NDiscLine, Socket and Modular Cont doesn't have output ports
    // * Метод работает некорректно для 3-фазного/3 однофазных реле напряжения, так как в них отсутствует выход на ноль
    // * Модульный контактор создается без соединений - так и должно быть?
    // */
    [TestCaseSource(nameof(ConfigureShieldCases))]
    public void CreateConnectionsTest(string caseName, Project testProject, int componentCountPhase1, int componentCountPhase3, int connectionCount, List<Component> uzos)
    {
        ConfigurationService config = new ConfigurationService(testProject);
        Project project = new Project();
        int amper = 0;
        var phaseCount = testProject.InitialSettings.ShieldWidth;
        CreatePortsCombination(config);

        config.ConfigureShield();
        config.shieldModuleSet.AddRange(uzos);

        Console.WriteLine("Enter point");
        config.CreateConnections();

        Assert.That(testProject.FuseBox.CableConnections.Count, Is.EqualTo(connectionCount));

    }

    //Вспомогательный метод для создания портов компонентов config.shieldModuleSet
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
