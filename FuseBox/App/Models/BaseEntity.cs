using Newtonsoft.Json;

namespace FuseBox
{
    public abstract class BaseEntity
    {
        [JsonProperty(Order = 1)]
        public int Id { get; set; }

        [JsonProperty(Order = 2)]
        public string? Name { get; set; }
    }

}
