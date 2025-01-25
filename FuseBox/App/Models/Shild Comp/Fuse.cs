using Newtonsoft.Json;

namespace FuseBox
{
    public class Fuse : Component
    {
        [JsonProperty(Order = 8)]
        public List<Consumer> Consumers { get; set; } = new(); // List of Equipment

        public Fuse(string? name, int amper, int slots, bool isCritical, int price, bool phases) //List<Consumer> consumers
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
            Phases3 = phases;
            // Consumers = consumers;
        }
    }
}
