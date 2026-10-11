using FuseBox.App.Models.BaseAbstract;

namespace FuseBox.App.Models.Shild_Comp
{
    public class CableDTO : BaseEntity
    {
        public decimal Length { get; set; } // m
        public decimal Section { get; set; } // mm^2
        public string? Сolour { get; set; }

        public CableDTO() { }

        public CableDTO(ConnectorColour colour, decimal section)
        {
            Сolour = Convert.ToString(colour);
            Section = section;
            //this.colour = Convert.ToString(Colour);
        }
    }
}
