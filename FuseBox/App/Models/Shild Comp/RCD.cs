using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;
using System.Xml.Linq;
using static System.Reflection.Metadata.BlobBuilder;

namespace FuseBox
{
    public class RCD : Component, IHasConsumer
    {
        public List<Component> Electricals { get; set; } = new List<Component>();

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }

        public const decimal LimitOfConnectedFuses = 5;

        [JsonProperty(Order = 4)]
        public double TotalLoad { get; set; } // Added in DistributeFusesToRCDs

        public RCD(string name, int amper, int slots, decimal price, List<Component> electricals) : base(name, amper, slots, price) // List<Electricals> electricals,
        {

            // В список разьемов добавляем разьемы с выходом для АВ - красного цвета и фазой с нолем на вход
            Ports = new List<Port> 
            {
                new Port(PortOutEnum.Phase1, ConnectorColour.Red), 
                new Port(PortOutEnum.Zero,   ConnectorColour.Blue), 
                //new Port(PortOut.AV,     new Cable (ConnectorColour.Red, (decimal)1.5  )) 
            };
            Capacity = 30;
            Electricals = electricals;
        }

        public RCD(string name, int amper, int slots, decimal price, List<Port> ports, List<Component> electricals) : base(name, amper, slots, price, ports) // List<Electricals> electricals,
        {

            // В список разьемов добавляем разьемы с выходом для АВ - красного цвета и фазой с нолем на вход
            Ports = ports;
            Capacity = 30;
            Electricals = electricals;
        }

        public RCD() { }
    }
}
