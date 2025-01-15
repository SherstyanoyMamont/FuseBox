namespace FuseBox
{
    // Added class Fuse
    public class Fuse
    {
        private static int _idCounter = 0; // Static counter for all objects of this class
        public int Id { get; private set; } // Assigned automatically
        public string? Name { get; set; }
        public int Amper { get; set; }
        public double Slots { get; set; }
        public int Price { get; set; }

        public List<Consumers> Equipments { get; set; } = new(); // List of Equipment

        public Fuse(string? name, int amper, int slots, bool isCritical, int price)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
    }

    public interface IMonitorable
    {
        void Monitor();
    }

    public class CircuitBreaker : Fuse, IMonitorable
    {
        public List<Consumers> Equipments { get; set; } = new(); // List of Equipment
        public bool IsCritical { get; set; } // Critical line (non-switchable)
        public VoltageRelay(string name, int rating) : base(name, rating) { }

        public void Monitor()
        {
            Console.WriteLine($"{Name} is monitoring voltage.");
        }
    }
}
