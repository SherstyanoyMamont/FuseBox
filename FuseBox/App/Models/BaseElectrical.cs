using Newtonsoft.Json;

namespace FuseBox.App.Models
{
    // [JsonConverter(typeof(Newtonsoft.Json.Converters.CustomCreationConverter<BaseElectrical>))]
    public abstract class BaseElectrical : BaseEntity
    {
        [JsonProperty(Order = 3)]
        public int Amper { get; set; }
    }
}
