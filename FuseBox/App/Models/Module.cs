namespace FuseBox
{
    public class Module : Component
    {
        private static int _idCounter = 0; // Static counter for all objects of this class

        // public List<Consumers> Equipments { get; set; } = new(); // List of Equipment

        public Module(string? name, int amper, int slots, bool isCritical, int price)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
    }

}
