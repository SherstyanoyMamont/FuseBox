using Newtonsoft.Json;

namespace FuseBox
{
    public class RCDFire : Component
    {

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }

        public RCDFire(string name, int amper, int slots, decimal price, List<Port> ports) : base(name, amper, slots, price, ports)
        {
            Ports = ports;
            Capacity = 300;
            base.Price = price;
        }

        public RCDFire(string name, int amper, int slots, decimal price) : base(name, amper, slots, price)
        {
            Capacity = 300;
            base.Price = price;
        }
    }
}
