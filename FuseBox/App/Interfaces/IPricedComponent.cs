using Newtonsoft.Json;

namespace FuseBox.App.Interfaces
{
    public interface IPricedComponent
    {
        [JsonProperty(Order = 5)]
        public decimal Price { get; set; }
    }
}
