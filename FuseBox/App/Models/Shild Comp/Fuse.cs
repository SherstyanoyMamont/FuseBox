using FuseBox.App.Interfaces;
using FuseBox.App.Models;
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
            //Connectors = new List<Connector> { connector };

            // В список разьемов добавляем разьем с входом для АВ и кабелем красного цвета
            Connectors = new List<Connector> { new Connector(ConnectorIn.AV, new Cable(ConnectorColour.Red, (decimal)1.5)) }; 
            Electricals = electricals;
        }
    }
}
