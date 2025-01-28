using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using Newtonsoft.Json;
using static System.Reflection.Metadata.BlobBuilder;
using System.Xml.Linq;

namespace FuseBox
{
    public class Contactor : Component, IHasConsumer
    {
        public List<BaseElectrical> Electricals { get; set; } = new List<BaseElectrical>();
        public Contactor(string name, int amper, int slots, int poles, decimal price, List<BaseElectrical> electricals) : base(name, amper, slots, poles, price)
        {
            Electricals = electricals;
        }
    }
}
