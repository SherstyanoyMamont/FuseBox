using Microsoft.AspNetCore.Rewrite;
using System;
using System.Collections.Generic;

namespace FuseBox
{
    // Added interface IFuse
    public interface IFuse
    {
        int Id { get; set; } // Assigned automatically
        string? Name { get; set; }
        int Amper { get; set; }
        double Slots { get; set; }
        int Price { get; set; }
    }


    // Interface for machines that have connected equipment
    public interface IFuseWithEquipment : IFuse
    {
        List<Consumers> Equipments { get; set; }
    }


    public class SimpleFuse : IFuse
    {
        private static int _idCounter = 0; // Static counter for all objects of this class
        public int Id { get; set; } // Assigned automatically // Private set?
        public string? Name { get; set; }
        public int Amper { get; set; }
        public double Slots { get; set; }
        public int Price { get; set; }

        // public List<Consumers> Equipments { get; set; } = new(); // List of Equipment

        public SimpleFuse(string? name, int amper, int slots, bool isCritical, int price)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
    }

    public class FuseWithEquipment : IFuseWithEquipment
    {
        private static int _idCounter = 0; // Static counter for all objects of this class
        public int Id { get; set; } // Assigned automatically // Private set?
        public string? Name { get; set; }
        public int Amper { get; set; }
        public double Slots { get; set; }
        public int Price { get; set; }

        public List<Consumers> Equipments { get; set; } = new(); // List of Equipment

        public FuseWithEquipment(string? name, int amper, int slots, bool isCritical, int price)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
    }

}
