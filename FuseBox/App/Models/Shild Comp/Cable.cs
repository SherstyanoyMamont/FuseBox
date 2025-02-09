using FuseBox.App.Interfaces;
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
    public class Cable : NonElectrical
    {
        public decimal Length { get; set; } // m
        public decimal Section { get; set; } // mm^2
        public string? colour1 { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConnectorColour Colour { get; set; }

        public Cable(ConnectorColour colour, decimal section) 
        {
            Colour = colour;
            Section = section;
            colour1 = Convert.ToString(Colour);

        }
    }
}
