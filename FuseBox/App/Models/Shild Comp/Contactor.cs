using Newtonsoft.Json;

namespace FuseBox
{
    public class Contactor : Component
    {
        //[JsonProperty(Order = 8)]
        //public List<Consumer> Consumers { get; set; } = new(); // List of Equipment

        [JsonProperty(Order = 9)]
        public List<Component> Components { get; set; } = new(); // List of Equipment

        public Contactor() //List<Consumer> consumers
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = "Contactor";
            Amper = 16;
            Slots = 2;
            Price = 50;
            Phases3 = false;
            // Consumers = consumers;
        }
    }
}
