using FuseBox.App.Interfaces;

namespace FuseBox.App.Models.Shild_Comp
{
    public class Cabel : NonElectrical, IPricedComponent
    {
        public decimal Price { get; set; } // $
        public decimal Length { get; set; } // m
        public int Section { get; set; } // mm^2
    }
}
