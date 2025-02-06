using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;

namespace FuseBox
{
    public class Component : BaseElectrical, IPricedComponent // Abstract?
    {
        // public static int _idCounter = 0; // Static counter for all objects of this class
        public decimal Price { get; set; }

        [JsonProperty(Order = 4)]
        public List<Connector> Connectors { get; set; }

        [JsonProperty(Order = 5)]
        public double Slots { get; set; }

        public Component(string name, int amper, List<Connector> connectors,  int slots, decimal price)
        {
            Id = ++_idCounter;
            Connectors = connectors;
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
        public Component(string name, int amper,int slots, decimal price)
        {
            Id = ++_idCounter;
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
    }
}
