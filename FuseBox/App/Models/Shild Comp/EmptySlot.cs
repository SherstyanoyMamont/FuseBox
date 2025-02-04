using FuseBox.App.Models;
using Newtonsoft.Json;

namespace FuseBox
{
    internal class EmptySlot : BaseElectrical
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