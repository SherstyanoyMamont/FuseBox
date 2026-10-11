using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts.Projects
{
    public sealed class ProjectSaveRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public FloorGroupingRequest FloorGrouping { get; set; } = new();

        [Required]
        public GlobalGroupingRequest GlobalGrouping { get; set; } = new();

        [Required]
        public InitialSettingsRequest InitialSettings { get; set; } = new();

        [Required]
        public FuseBoxSettingsRequest FuseBox { get; set; } = new();

        public List<FloorRequest> Floors { get; set; } = new();
    }
}
