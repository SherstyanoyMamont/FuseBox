using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;

namespace FuseBox.App.Models.DTO
{
    public class RoomDTO : BaseNonElectrical, IZone
    {
        // public int Rating { get; set; }                // ... 10А, 16А, 20А ...
        public List<ConsumerDTO> Consumer { get; set; } = new List<ConsumerDTO>();

        public RoomDTO() { }

    }
}
