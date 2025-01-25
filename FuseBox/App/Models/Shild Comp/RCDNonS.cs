using Newtonsoft.Json;

namespace FuseBox
{
    public class RCDNonS : BaseRCD
    {
        [JsonProperty(Order = 9)]
        public List<Consumer> Consumers { get; set; } = new(); // List of Equipment

        public RCDNonS(string? name, int amper, int slots, bool isCritical, int price, int capacity, List<Consumer> consumer, bool phases3)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
            Capacity = capacity;
            Consumers = consumer;
            Phases3 = phases3;
        }
    }
}
