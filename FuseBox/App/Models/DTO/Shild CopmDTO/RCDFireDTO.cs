using FuseBox.App.Models.DTO;
using FuseBox.App.Models.DTO.ConfugurationDTO;
using Newtonsoft.Json;

namespace FuseBox
{
    public class RCDFireDTO : ComponentDTO
    {

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }


        public RCDFireDTO() { }

    }
}
