namespace FuseBox
{
    // Added class Fuse
    public class Fuse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Slots { get; set; }
        public int Rating { get; set; }
        public bool IsCritical { get; set; } // Critical line (non-switchable)

        public Fuse(int id, string? name, int slots, int rating, bool isCritical = false)
        {
            Id = id;
            Name = name;
            Slots = slots;
            Rating = rating;
            IsCritical = isCritical;
        }

        public override string ToString()
        {
            return $"{Name}: {Id} {Rating}A {Slots} (Critical: {IsCritical})";
        }
    }
}
