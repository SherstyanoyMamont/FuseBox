using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using Newtonsoft.Json;
using System.Xml.Linq;
using static System.Reflection.Metadata.BlobBuilder;

namespace FuseBox
{
    public class RCD : Component, IHasConsumer
    {
        public List<BaseElectrical> Electricals { get; set; } = new List<BaseElectrical>();

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }

        public const decimal LimitOfConnectedFuses = 5;

        public RCD(string name, int amper, int slots, int poles, decimal price, int capacity, List<BaseElectrical> electricals) : base(name, amper, slots, poles, price) // List<Electricals> electricals,
        {
            Capacity = capacity;
            Electricals = electricals;
            
        }
    }
}
