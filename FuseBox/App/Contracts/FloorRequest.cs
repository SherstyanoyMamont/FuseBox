using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts.Projects
{
    public sealed class FloorRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        public List<RoomRequest> Rooms { get; set; } = new();
    }
}
