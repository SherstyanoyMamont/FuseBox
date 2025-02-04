using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using Newtonsoft.Json;
using static System.Reflection.Metadata.BlobBuilder;
using System.Xml.Linq;
using FuseBox.App.Models.Shild_Comp;

namespace FuseBox
{
    public class Contactor : Component, IHasConsumer
    {
        public List<BaseElectrical> Electricals { get; set; } = new List<BaseElectrical>();

        public Contactor(string name, int amper, int slots, decimal price, List<BaseElectrical> electricals) : base(name, amper, slots, price)
        {
            Connectors = new List<Connector>();
            Electricals = electricals;

        }
    }
}
