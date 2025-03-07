using FuseBox.App.Models.BaseAbstract;
using Newtonsoft.Json;

namespace FuseBox
{
    public class EmptySlot : BaseElectrical           // изменил с internal на public для тестов
    {
        [JsonProperty(Order = 4)]
        public int Slots { get; set; }

        public EmptySlot(int slots)
        {
            Name = "Empty Slot";
            Slots = slots;
        }
    }
}