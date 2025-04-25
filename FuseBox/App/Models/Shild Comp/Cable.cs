using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FuseBox.App.Models.Shild_Comp
{
    public enum ConnectorColour
    {
        Red,
        Orange,
        Grey,
        Blue
    }

    public class Cable : BaseEntity
    {
        public decimal Length { get; set; } // m
        public decimal Section { get; set; } // mm^2
        public string? Сolour { get; set; }

        // Связь с Connection
        public int? ConnectionCableId { get; set; }
        [JsonIgnore]
        [ForeignKey("ConnectionCableId")]
        public CableConnection? Connection { get; set; }

        public Cable() { }

        public Cable(ConnectorColour colour, decimal section) 
        {
            Сolour = Convert.ToString(colour);
            Section = section;
            //this.colour = Convert.ToString(Colour);
        }
    }
}
