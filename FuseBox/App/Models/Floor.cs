// Am i Join tp this rep?
using FuseBox.App.Models.BaseAbstract;
using System.Text.Json.Serialization;

namespace FuseBox
{
    public class Floor : BaseNonElectrical
    {
        // public int Power { get; set; } // Add Floor Power
        public List<Room> Rooms { get; set; } = new();

        // Связь с проектом
        public int ProjectId { get; set; }
        [JsonIgnore]
        public Project Project { get; set; }

        public Floor() { }

        public Floor(List<Room> rooms)
        {
            Rooms = rooms;
        }
    }
}
