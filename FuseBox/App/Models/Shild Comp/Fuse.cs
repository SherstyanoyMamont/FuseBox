using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;

namespace FuseBox
{
    public class Fuse : Component, IHasConsumer
    {
        [JsonProperty(Order = 8)]
        public List<BaseElectrical> Electricals { get; set; } = new List<BaseElectrical>();

        public Fuse(string name,  int amper, int slots, decimal price, List<BaseElectrical> electricals) : base(name, amper, slots, price)
        {
            // В список разьемов добавляем разьем с входом для АВ и кабелем красного цвета
            Ports = new List<Port> { new Port(PortIn.AV, new Cable(ConnectorColour.Red, (decimal)1.5)) }; 
            Electricals = electricals;
            //TotalLoad = totalLoad;
        }

        public double GetTotalLoad()
        {
            double totalLoad = 0;

            foreach (var item in Electricals)
            {
                totalLoad += item.Amper;
            }
            return totalLoad;
        }
    }
}
