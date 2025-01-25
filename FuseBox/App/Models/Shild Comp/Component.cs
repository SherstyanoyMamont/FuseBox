using Newtonsoft.Json;

namespace FuseBox
{
    public abstract class Component : BaseEntity
    {
        public static int _idCounter = 0; // Static counter for all objects of this class

        [JsonProperty(Order = 3)]
        public int Amper { get; set; }

        [JsonProperty(Order = 4)]
        public int Slots { get; set; }

        [JsonProperty(Order = 5)]
        public decimal Price { get; set; }

        [JsonProperty(Order = 6)]
        public int Poles { get; set; }

        [JsonProperty(Order = 7)]
        public bool Phases3 { get; set; }
    }
}
