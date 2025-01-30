using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using Newtonsoft.Json;

namespace FuseBox
{
    public class Component : BaseElectrical, IPricedComponent // Abstract?
    {
        public static int _idCounter = 0; // Static counter for all objects of this class

        [JsonProperty(Order = 4)]
        public int Slots { get; set; }

        [JsonProperty(Order = 6)]
        public int Poles { get; set; }
        public decimal Price { get; set; }

        public Component(string name, int amper, int slots, int poles, decimal price)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Poles = poles;
            Price = price;
        }
    }


   

    class Соединение
    {
        // ID
        // тип кабеля
        // индекс компонент1
        // выход/вход компонент1

        // индекс на компонент2
        // выход/вход компонент2

        // тип соединения ???
    }

}
