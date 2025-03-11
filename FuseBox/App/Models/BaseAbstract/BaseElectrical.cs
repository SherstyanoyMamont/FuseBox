using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Models.BaseAbstract
{
    // [JsonConverter(typeof(Newtonsoft.Json.Converters.CustomCreationConverter<BaseElectrical>))]
    public abstract class BaseElectrical : BaseEntity
    {
        [JsonProperty(Order = 3)]
        [Required(ErrorMessage = "Required field")]
        [Range(0, 100, ErrorMessage = "Amper from 0 to 100")]
        public double Amper { get; set; }
    }
}
