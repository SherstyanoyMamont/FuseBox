using Newtonsoft.Json;

namespace FuseBox
{
    public class RCDFire : BaseRCD
    {
        public RCDFire(string? name, int amper, int slots, bool isCritical, int price, int capacity, bool phases3)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
            Capacity = capacity;
            Phases3 = phases3;
        }
    }
}
