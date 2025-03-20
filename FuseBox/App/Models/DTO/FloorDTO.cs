// Am i Join tp this rep?

// Am i Join tp this rep?
using FuseBox.App.Models.BaseAbstract;

namespace FuseBox.App.Models.DTO
{
    public class FloorDTO : BaseNonElectrical
    {
        // public int Power { get; set; } // Add Floor Power
        public List<RoomDTO> Rooms { get; set; } = new();

        public FloorDTO() { }

    }
}
