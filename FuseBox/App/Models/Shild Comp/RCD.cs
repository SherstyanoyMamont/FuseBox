using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;
using System.Xml.Linq;
using static System.Reflection.Metadata.BlobBuilder;

namespace FuseBox
{
    public class RCD : Component, IHasConsumer
    {
        public List<BaseElectrical> Electricals { get; set; } = new List<BaseElectrical>();

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }

        public const decimal LimitOfConnectedFuses = 5;

        public RCD(string name, int amper, int slots, decimal price, int capacity, List<BaseElectrical> electricals) : base(name, amper, slots, price) // List<Electricals> electricals,
        {

            // В список разьемов добавляем разьемы с выходом для АВ - красного цвета и фазой с нолем на вход
            Connectors = new List<Connector> 
            {
                new Connector(ConnectorIn.Phase1, new Cable (ConnectorColour.Red, (decimal)10.00)), 
                new Connector(ConnectorIn.Zero,  new Cable (ConnectorColour.Blue,(decimal)10.00)), 
                new Connector(ConnectorOut.AV,  new Cable (ConnectorColour.Red, (decimal)1.5  )) 
            };
            Capacity = capacity;
            Electricals = electricals;
        }

        public RCD(string name, int amper, List<Connector> connectors, int slots, decimal price, int capacity, List<BaseElectrical> electricals) : base(name, amper, connectors, slots, price) // List<Electricals> electricals,
        {

            // В список разьемов добавляем разьемы с выходом для АВ - красного цвета и фазой с нолем на вход
            Connectors = connectors;
            Capacity = capacity;
            Electricals = electricals;
        }

        public void OrderBreakersId()
        {
            for (int i = 0; i < Electricals.Count; i++)
            {
                Electricals[i].Id = i + 1;
            }
        }
    }
}
