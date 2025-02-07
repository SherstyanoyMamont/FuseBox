using Newtonsoft.Json;

namespace FuseBox
{
    public class RCDFire : Component
    {

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }

        public RCDFire(string name, int amper, List<Port> ports, int slots, decimal price, int capacity) : base(name, amper, ports, slots, price)
        {
            Ports = ports;
            Capacity = capacity;
            base.Price = price;
        }

        public RCDFire(string name, int amper, int slots, decimal price, int capacity) : base(name, amper, slots, price)
        {
            Capacity = capacity;
            base.Price = price;
        }
    }
}
