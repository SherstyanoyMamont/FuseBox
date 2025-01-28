using FuseBox.App.Models;
using Newtonsoft.Json;

namespace FuseBox.App.Interfaces
{
    public interface IHasConsumer
    {
        [JsonProperty(Order = 9)]
        public List<BaseElectrical> Electricals { get; set; }
    }
}
