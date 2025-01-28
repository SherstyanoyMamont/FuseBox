using Newtonsoft.Json;

namespace FuseBox
{
    public class RCDFire : Component
    {

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }

        public RCDFire(int capacity, string name, int amper, int slots, int poles, decimal price) : base(name, amper, slots, poles, price)
        {
            Capacity = capacity;
        }
    }
}
