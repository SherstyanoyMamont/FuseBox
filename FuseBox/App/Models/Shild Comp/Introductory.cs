using FuseBox.App.Interfaces;
using Newtonsoft.Json;

namespace FuseBox
{
    public enum Type3PN
    {
        P1,
        P3,
        P3_N
    }
    public class Introductory : Component
    {
        public Type3PN Type { get; set; }

        public Introductory(string name, Type3PN type, int slots, int amper,  decimal price, string type3PN) : base(name, amper, slots, price)
        {
            Type = type;
        }
    }
}
