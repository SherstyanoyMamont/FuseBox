using Newtonsoft.Json;

namespace FuseBox
{
    public class RCD : BaseRCD
    {
        [JsonProperty(Order = 9)]
        public List<Fuse> FusesGroup { get; set; } = new(); // List of Equipment

        public RCD(string? name, int amper, int slots, bool isCritical, int price, int capacity, bool phases3) // List<Fuse> fusesGroup,
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
            Capacity = capacity;
            // FusesGroup = fusesGroup;
            Phases3 = phases3;
        }
    }
}
