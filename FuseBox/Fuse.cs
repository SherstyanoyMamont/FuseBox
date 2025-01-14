namespace FuseBox
{
    // Added class Fuse
    public class Fuse
    {
        private static int _idCounter = 0; // Static counter for all objects of this class
        public int Id { get; private set; } // Assigned automatically
        public string? Name { get; set; }
        public int Amper { get; set; }
        public int Slots { get; set; }
        public bool Area { get; set; } // Wet or dry zone
        public bool IsCritical { get; set; } // Critical line (non-switchable)
        public int Price { get; set; }

        public Fuse(string? name, int amper, int slots, bool area, bool isCritical, int price)
        {
            Id = ++_idCounter; // Increment the counter and assign the ID
            Name = name;
            Amper = amper;
            Slots = slots;
            Area = area;
            IsCritical = isCritical;
            Price = price;
        }

        public override string ToString()
        {
            return $"{Name}: {Id} {Area}A {Slots} (Critical: {IsCritical})";
        }
    }
}
