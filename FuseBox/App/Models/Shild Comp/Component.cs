using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;

namespace FuseBox
{
    public class Component : BaseElectrical, IPricedComponent // Abstract?
    {
        public int SerialNumber { get; set; } // Serial number of the component
        public decimal Price { get; set; }

        // !!! Скрытые списки разьемов
        [JsonIgnore]
        [JsonProperty(Order = 8)]

        public List<Port>? Ports = new List<Port>();

        [JsonProperty(Order = 5)]
        public int Slots { get; set; }


        //Связь с группой компонентов
        public int FuseBoxComponentGroupId { get; set; }
        [JsonIgnore]
        public FuseBoxComponentGroup? FuseBoxComponentGroup { get; set; }

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
        public Component(string name, int slot)     // Для тестов
        {
            this.Name = name;
            this.Slots = slot;
        }
        public Component() { }      // Для тестов
    }
}
