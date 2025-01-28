using Newtonsoft.Json;

namespace FuseBox.App.Models
{
    public abstract class BaseElectrical : BaseEntity
    {
        [JsonProperty(Order = 3)]
        public int Amper { get; set; }
    }
}
