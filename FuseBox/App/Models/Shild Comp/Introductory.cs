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

        public Introductory(string name, Type3PN type, List<Port> ports, int slots, int amper,  decimal price, string type3PN) : base(name, amper, ports, slots, price)
        {
            Ports = ports;
            Type = type;
        }
    }
}
