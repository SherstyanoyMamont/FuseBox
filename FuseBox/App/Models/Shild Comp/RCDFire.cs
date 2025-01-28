using Newtonsoft.Json;

namespace FuseBox
{
    public class RCDFire : Component
    {

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }

        public RCDFire(string name, int amper, int slots, int poles, decimal price, int capacity) : base(name, amper, slots, poles, price)
        {
            Capacity = capacity;
        }
    }
}
