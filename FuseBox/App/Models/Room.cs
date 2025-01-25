// Am i Join tp this rep?
namespace FuseBox
{
    public class Room : BaseEntity
    {
        private static int _idCounter = 0;             // Static counter for all objects of this class
        public int Rating { get; set; }                // ... 10А, 16А, 20А ...
        public int TPower { get; set; }                // Calculated power of all equipment in the room
        public bool Area { get; set; }                 // "Dry" or "Wet"
        public List<Consumer> Consumer { get; set; }   // Load list

    }
}
