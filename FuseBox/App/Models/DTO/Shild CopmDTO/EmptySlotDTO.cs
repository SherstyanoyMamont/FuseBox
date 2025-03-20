using FuseBox.App.Models.DTO;
using Newtonsoft.Json;

namespace FuseBox
{
    public class EmptySlotDTO : ComponentDTO          // изменил с internal на public для тестов
    {
        [JsonProperty(Order = 4)]
        public new int Slots { get; set; }


        public EmptySlotDTO() { }
    }
}