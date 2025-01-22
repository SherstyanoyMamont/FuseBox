// Am i Join tp this rep?
namespace FuseBox
{
    public class Room
    {
        private static int _idCounter = 0;             // Static counter for all objects of this class
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool Area { get; set; }                 // "Dry" or "Wet"
        public int Rating { get; set; }                // ... 10А, 16А, 20А ...
        public List<Consumer> Equipments { get; set; } // Load list
        public int TPower { get; set; }                // Calculated power of all equipment in the room
        
    }
}
