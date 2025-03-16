using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;

namespace FuseBox
{
    public class Fuse : Component
    {
        [JsonProperty(Order = 8)]
        public List<Consumer> Electricals { get; set; } = new List<Consumer>();

        public Fuse(string name,  int amper, int slots, decimal price, List<Consumer> electricals) : base(name, amper, slots, price)
        {
            // В список разьемов добавляем разьем с входом для АВ и кабелем красного цвета
            Ports = new List<Port> { new Port(PortInEnum.AV, new Cable(ConnectorColour.Red, (decimal)1.5)) }; 
            Electricals = electricals;
            //TotalLoad = totalLoad;
        }

        public Fuse() { }

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
