using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;

namespace FuseBox
{
    public enum PortIn
    {
        Unknown = 0,   // Значение по умолчанию (если не заполнено)
        Phase1,
        Phase2,
        Phase3,
        Zero,
        AV,
    }
    public enum PortOut
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
        public PortOut portOut { get; set; } // Выход
        public PortIn PortIn { get; set; } // Вход

        public Cable cableType;

        public Port(PortOut portOut, PortIn portIn, Cable cableType)
        {
            this.portOut = portOut;
            this.PortIn = portIn;
            this.cableType = cableType;
        }

        public Port(PortOut connectorType, Cable cableType)
        {
            this.portOut = connectorType;
            this.cableType = cableType;
        }

        public Port(PortIn portIn, Cable cableType)
        {
            this.PortIn = portIn;
            this.cableType = cableType;
        }
    }
}