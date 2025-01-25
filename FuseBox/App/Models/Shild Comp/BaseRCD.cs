using Newtonsoft.Json;

namespace FuseBox
{
    public abstract class BaseRCD : Component
    {
        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }
    }
}
