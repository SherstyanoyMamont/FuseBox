using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;

namespace FuseBox
{
    public class Component : BaseElectrical, IPricedComponent // Abstract?
    {
        // public static int _idCounter = 0; // Static counter for all objects of this class
        public decimal Price { get; set; }

        // !!! Скрытые списки разьемов
        //[JsonIgnore]
        [JsonProperty(Order = 8)]

        public List<Port> Ports = new List<Port>();

        [JsonProperty(Order = 5)]
        public int Slots { get; set; }

        public Component(string name, int amper, int slots, decimal price, List<Port> ports)
        {
            Ports = ports;
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
        public Component(string name, int amper, int slots, decimal price)
        {
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
    }
}
