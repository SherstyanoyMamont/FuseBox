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
        public List<BaseElectrical> Electricals { get; set; } = new List<BaseElectrical>();

        public Contactor(string name, int amper, int slots, decimal price, List<Port> ports, List<BaseElectrical> electricals) : base(name, amper, slots, price)
        {
            Ports = ports;
            Electricals = electricals;
        }

        public Contactor(string name, int amper, int slots, decimal price, List<BaseElectrical> electricals) : base(name, amper, slots, price)
        {
            Electricals = electricals;
        }
        public Contactor(string name, int slots)
        {
            this.Name = name;
            this.Slots = slots;
        }
    }
}
