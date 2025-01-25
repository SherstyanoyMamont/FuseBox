using Newtonsoft.Json;

namespace FuseBox
{
    public class Introductory : Component
    {
        private bool _isDependentOption;

        [JsonProperty(Order = 8)]
        public bool Type3PN { get; set; }


        public Introductory(string? name, int amper, int slots, bool isCritical, int price, bool type3PN, bool phases3)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
            Phases3 = phases3;
            Type3PN = type3PN;
        }
    }
}
