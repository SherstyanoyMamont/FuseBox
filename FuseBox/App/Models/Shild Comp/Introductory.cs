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
        public string? Type { get; set; }

        public Introductory(string name, int amper, int slots, decimal price, List<Port> ports, string type3PN, Type3PN type) : base(name, amper, slots, price, ports)
        {
            Ports = ports;
            Type = Convert.ToString(type);
        }
        public Introductory(string name, Type3PN type, int slots)   // Для тестов    
        {
            this.Name = name;
            this.Type = Convert.ToString(type);
            this.Slots = slots;
        }

        public Introductory() { }
    }
}
