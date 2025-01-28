using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using Newtonsoft.Json;

namespace FuseBox
{
    public class Fuse : Component, IHasConsumer
    {
        [JsonProperty(Order = 8)]
        public List<BaseElectrical> Electricals { get; set; } = new List<BaseElectrical>();

        public Fuse(string name, int amper, int slots, int poles, decimal price/*, List<BaseElectrical> electricals*/) : base(name, amper, slots, poles, price)
        {
            /*Electricals = electricals;*/
        }
    }
}
