using FuseBox.App.Interfaces;

namespace FuseBox.App.Models.Shild_Comp
{
    public enum ConnectorColour
    {
        Red,
        Orange,
        Grey,
        Blue
    }
    public class Cable : NonElectrical, IPricedComponent
    {
        public decimal Price { get; set; } // $
        public decimal Length { get; set; } // m
        public decimal Section { get; set; } // mm^2
        public string Type { get; set; } // Тип кабеля
        public ConnectorColour Colour { get; set; }

        public Cable(ConnectorColour colour, decimal section) 
        {
            Colour = colour;
            Section = section;

        }
    }
}
