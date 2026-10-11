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

        //// Обратная связь с FuseBoxUnit
        //public int FuseBoxComponentGroupId { get; set; }
        //public FuseBoxComponentGroup? FuseBoxComponentGroup { get; set; }

        //// Обратная связь с группой компонентов
        //public int ComponentId { get; set; }
        //public Component? Component { get; set; }

        //// Обратная связь с Position
        //public int PositionId { get; set; }
        //public Position? Position { get; set; }
    }
}
