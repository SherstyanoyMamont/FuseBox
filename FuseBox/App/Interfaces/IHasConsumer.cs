using FuseBox.App.Models.BaseAbstract;
using Newtonsoft.Json;

namespace FuseBox.App.Interfaces
{
    public interface IHasConsumer
    {
        [JsonProperty(Order = 9)]
        public List<BaseElectrical> Electricals { get; set; }
    }
}
