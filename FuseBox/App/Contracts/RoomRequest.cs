using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts.Projects
{
    public sealed class RoomRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        public List<ConsumerRequest> Consumers { get; set; } = new();
    }
}
