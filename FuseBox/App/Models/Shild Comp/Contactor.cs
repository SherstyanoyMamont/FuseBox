using FuseBox.App.Interfaces;
using Newtonsoft.Json;
using static System.Reflection.Metadata.BlobBuilder;
using System.Xml.Linq;
using FuseBox.App.Models.Shild_Comp;
using FuseBox.App.Models.BaseAbstract;

namespace FuseBox
{
    public class Contactor : Component, IHasConsumer
    {
        public List<Component> Electricals { get; set; } = new List<Component>();

        public Contactor(string name, int amper, int slots, decimal price, List<Port> ports) : base(name, amper, slots, price)
        {
            Ports = ports;
        }

        public Contactor(string name, int amper, int slots, decimal price) : base(name, amper, slots, price)
        {
        }

        public Contactor(string name, int slots)
        {
            this.Name = name;
            this.Slots = slots;
        }

        public Contactor() { }
    }
}
