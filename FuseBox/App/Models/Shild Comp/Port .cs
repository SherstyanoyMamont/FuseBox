using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json.Converters;

namespace FuseBox
{
    public enum PortInEnum
    {
        Unknown = 0,   // Значение по умолчанию (если не заполнено)
        Phase1,
        Phase2,
        Phase3,
        Zero,
        AV,            // Имееться ввиду разьем только для AV автоматов
    }
    public enum PortOutEnum
    {
        Unknown = 0,   // Значение по умолчанию (если не заполнено)
        Phase1,
        Phase2,
        Phase3,
        Zero,
        AV,
    }

    public class Port : BaseEntity
    {
        public int connectionsCount {  get; set; }
        public string portOut { get; set; }
        public string PortIn { get; set; }


        public Cable cableType;

        public Port(PortOutEnum connectorType, Cable cableType)
        {
            this.portOut = Convert.ToString(connectorType);
            this.cableType = cableType;
        }

        public Port(PortInEnum portIn, Cable cableType)
        {
            this.PortIn = Convert.ToString(portIn);
            this.cableType = cableType;
        }

        public Port() { }


        // Фабричный метод для создания стандартного набора портов
        public static List<Port> CreateStandardPorts(decimal wireSection)
        {
            var portPairs = new[]
            {
                (PortInEnum.Phase1, PortOutEnum.Phase1, ConnectorColour.Red),
                (PortInEnum.Phase2, PortOutEnum.Phase2, ConnectorColour.Orange),
                (PortInEnum.Phase3, PortOutEnum.Phase3, ConnectorColour.Grey),
                (PortInEnum.Zero,   PortOutEnum.Zero,   ConnectorColour.Blue)
            };

            var ports = new List<Port>();

            foreach (var (portIn, portOut, colour) in portPairs)
            {
                var cable = new Cable(colour, wireSection);
                ports.Add(new Port(portIn, cable));
                ports.Add(new Port(portOut, cable));
            }

            return ports;
        }
    }
}