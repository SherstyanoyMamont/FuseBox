namespace FuseBox
{
    public class Fuse : Component
    {
        private static int _idCounter = 0; // Static counter for all objects of this class

        public List<Consumer> Consumers { get; set; } = new(); // List of Equipment

        public Fuse(string? name, int amper, int slots, bool isCritical, int price) //List<Consumer> consumers
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
            // Consumers = consumers;
        }
    }
}
