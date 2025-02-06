using FuseBox.App.Models.Shild_Comp;

namespace FuseBox
{
    public enum ConnectorIn
    {
        Unknown = 0,   // Значение по умолчанию (если не заполнено)
        Phase1,
        Phase2,
        Phase3,
        Zero,
        AV,
    }
    public enum ConnectorOut
    {
        Unknown = 0,   // Значение по умолчанию (если не заполнено)
        Phase1,
        Phase2,
        Phase3,
        Zero,
        AV,
    }

    public class Connector : BaseEntity
    {
        public int connectionsCount {  get; set; }
        public ConnectorOut connectorOut { get; set; } // Выход
        public ConnectorIn connectorIn { get; set; } // Вход

        public Cable cableType;

        public Connector(ConnectorOut connectorOut, ConnectorIn connectorIn, Cable cableType)
        {
            this.connectorOut = connectorOut;
            this.connectorIn = connectorIn;
            this.cableType = cableType;
        }

        public Connector(ConnectorOut connectorType, Cable cableType)
        {
            this.connectorOut = connectorType;
            this.cableType = cableType;
        }

        public Connector(ConnectorIn connectorIn, Cable cableType)
        {
            this.connectorIn = connectorIn;
            this.cableType = cableType;
        }
    }
}