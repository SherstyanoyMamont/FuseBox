using FuseBox.App.Interfaces;
using Newtonsoft.Json;

namespace FuseBox
{
    public class Introductory : Component
    {
        public enum Type3PN 
        {
            P1,
            P3,
            P3_N
        }

        public Introductory(string name, int amper, int slots, int poles, decimal price, string type3PN) : base(name, amper, slots, poles, price)
        {
            Type3PN type3PN1 = new Type3PN();

        }
    }
}
