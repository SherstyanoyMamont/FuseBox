namespace FuseBox
{
    public class Fuse : IFuseWithEquipment
    {
        private static int _idCounter = 0; // Static counter for all objects of this class
        public int Id { get; set; }        // Assigned automatically // Private set?
        public string? Name { get; set; }
        public int Amper { get; set; }
        public double Slots { get; set; }
        public int Price { get; set; }
        public List<Consumer> Equipments { get; set; } = new(); // List of Equipment

        public Fuse(string? name, int amper, int slots, bool isCritical, int price)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
    }
}
