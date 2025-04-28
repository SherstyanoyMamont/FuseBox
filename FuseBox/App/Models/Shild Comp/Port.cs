using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        public string? portOut { get; set; }
        public string? PortIn { get; set; }

        [NotMapped]
        public string? connectorColour { get; set; } // Цвет разьема

        // Связь с Cable
        public int CableId { get; set; }
        [JsonIgnore]
        public Cable cableType { get; set; }

        // Связь с Component
        public int ComponentId { get; set; }
        [JsonIgnore]
        public Component Component { get; set; }

        public Port(PortOutEnum connectorType, ConnectorColour cableColour)
        {
            this.portOut = Convert.ToString(connectorType);
            this.connectorColour = Convert.ToString(cableColour);
        }

        public Port(PortInEnum portIn, ConnectorColour cableColour)
        {
            this.PortIn = Convert.ToString(portIn);
            this.connectorColour = Convert.ToString(cableColour);
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
                //var cable = new Cable(colour, wireSection); // Зачем нам куча ненужных кабелей?
                ports.Add(new Port(portIn, colour));
                ports.Add(new Port(portOut, colour));
            }

            return ports;
        }
    }
}