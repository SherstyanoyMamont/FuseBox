using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;

namespace FuseBox.App.Models.DTO.ConfugurationDTO
{
    public class PortDTO : BaseEntity
    {
        public int connectionsCount { get; set; }
        public string portOut { get; set; }
        public string PortIn { get; set; }


        public CableDTO cableType;

        public PortDTO(PortOutEnum connectorType, CableDTO cableType)
        {
            portOut = Convert.ToString(connectorType);
            this.cableType = cableType;
        }

        public PortDTO(PortInEnum portIn, CableDTO cableType)
        {
            PortIn = Convert.ToString(portIn);
            this.cableType = cableType;
        }

        public PortDTO() { }


        // Фабричный метод для создания стандартного набора портов
        public static List<PortDTO> CreateStandardPorts(decimal wireSection)
        {
            var portPairs = new[]
            {
                (PortInEnum.Phase1, PortOutEnum.Phase1, ConnectorColour.Red),
                (PortInEnum.Phase2, PortOutEnum.Phase2, ConnectorColour.Orange),
                (PortInEnum.Phase3, PortOutEnum.Phase3, ConnectorColour.Grey),
                (PortInEnum.Zero,   PortOutEnum.Zero,   ConnectorColour.Blue)
            };

            var ports = new List<PortDTO>();

            foreach (var (portIn, portOut, colour) in portPairs)
            {
                var cable = new CableDTO(colour, wireSection);
                ports.Add(new PortDTO(portIn, cable));
                ports.Add(new PortDTO(portOut, cable));
            }

            return ports;
        }
    }
}