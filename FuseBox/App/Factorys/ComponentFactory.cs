using FuseBox.App.Interfaces;
using System.Collections.Generic;

namespace FuseBox.App.Factorys
{
    public class ComponentFactory : IComponentFactory
    {
        public ComponentFactory() 
        {
            PortsPresets = ConfigureShield();
        }

        public List<Port> ports = new List<Port>(Port.CreateStandardPorts(10));
        public List<List<Port>> PortsPresets { get; set; } = new List<List<Port>>(); // Список всех разьемов
        public List<List<Port>> ConfigureShield()
        {
            List<List<Port>> Presets = new List<List<Port>>();

            List<Port> SelectPorts(params int[] indices) => indices.Select(i => ports[i]).ToList();

            Presets.Add(SelectPorts(0, 1, 6, 7));
            Presets.Add(SelectPorts(1, 7));
            Presets.Add(SelectPorts(1, 3, 5, 7));
            Presets.Add(SelectPorts(0, 1, 2, 3, 4, 5));
            Presets.Add(SelectPorts(0, 1, 2, 3, 4, 5, 7));
            Presets.Add(SelectPorts(0, 1, 2, 3, 4, 5, 6, 7));
            Presets.Add(SelectPorts(0, 1, 7));
            Presets.Add(SelectPorts(2, 3, 7));
            Presets.Add(SelectPorts(4, 5, 7));

            return Presets;
        }

        // 1P
        public Component CreateIntroductoryModule()
        {
            return new Component("Introductory", 63, 2, 35, PortsPresets[0]);
        }

        public Component CreateSurgeProtectionModule()
        {
            return new Component("SPD", 100, 2, 65, PortsPresets[1]);
        }

        public Component CreateLoadSwitchModule()
        {
            return new Component("LoadSwitch", 63, 2, 35, PortsPresets[0]);
        }
        public Component CreateRailMeterModule()
        {
            return new Component("DinRailMeter", 63, 2, 35, PortsPresets[0]);
        }
        public Component CreateFireUZOModule()
        {
            return new Component("RCDFire", 63, 2, 35, PortsPresets[0]);
        }
        public Component CreateVoltageRelayModule()
        {
            return new Component("VoltageRelay", 63, 2, 35, PortsPresets[0]);
        }
        public Component CreateRailSocketModule()
        {
            return new Component("DinRailSocket", 63, 2, 35, PortsPresets[1]);
        }
        public Component CreateNDiscLineModule()
        {
            return new Component("NDiscLine", 63, 2, 35, PortsPresets[1]);
        }

        public Component CreateModularContactorModule()
        {
            return new Component("ModularContactor", 63, 2, 35, PortsPresets[1]);
        }
        public Component CreateCrossBlockModule()
        {
            return new Component("CrossBlock", 63, 2, 35, PortsPresets[0]);
        }


        // 3P

        public Component CreateIntroductory3pModule()
        {
            return new Component("Introductory3p", 63, 3, 35, PortsPresets[3]);
        }
        public Component CreateIntroductory3pnModule()
        {
            return new Component("Introductory3pn", 63, 3, 35, PortsPresets[5]);
        }
        public Component CreateSPD3Module()
        {
            return new Component("SPD3", 63, 3, 35, PortsPresets[2]);
        }
        public Component CreateDinRailMeter3pModule()
        {
            return new Component("DinRailMeter3p", 63, 3, 35, PortsPresets[5]);
        }
        public Component CreateRCDFire3pModule()
        {
            return new Component("RCDFire3p", 63, 3, 35, PortsPresets[5]);
        }
        public Component CreateVoltageRelay2Module()
        {
            return new Component("VoltageRelay2", 63, 3, 35, PortsPresets[7]);
        }
        public Component CreateVoltageRelay3Module()
        {
            return new Component("VoltageRelay3", 63, 3, 35, PortsPresets[8]);
        }

        public Component CreateVoltageRelay3PModule()
        {
            return new Component("VoltageRelay3P", 63, 3, 35, PortsPresets[4]);
        }
    }
}
