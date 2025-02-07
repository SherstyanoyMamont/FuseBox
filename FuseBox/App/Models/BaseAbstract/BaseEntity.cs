using Newtonsoft.Json;

namespace FuseBox.App.Models.BaseAbstract
{
    public abstract class BaseEntity
    {
        [JsonProperty(Order = 1)] // Order of the properties in the JSON
        public int Id { get; set; }

        [JsonProperty(Order = 2)]
        public string? Name { get; set; }
    }
}
