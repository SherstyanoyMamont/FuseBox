using Newtonsoft.Json;

namespace FuseBox
{
    public class Module : Component
    {
        // public List<Consumers> Equipments { get; set; } = new(); // List of Equipment
        [JsonProperty(Order = 8)]
        public string Type { get; set; }

        public Module(string? name, int amper, int slots, bool isCritical, int price, bool phases3, string type)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
            Phases3 = phases3;
            Type = type;
        }
    }

}
