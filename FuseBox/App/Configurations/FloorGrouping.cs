using System.ComponentModel.DataAnnotations;

namespace FuseBox
{
    // Grouping by floors
    public class FloorGrouping
    {
        [Required(ErrorMessage = "Required field")]
        public bool IndividualFloorGrouping { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool SeparateUZOPerFloor { get; set; }

        // Связь с проектом
        public int ProjectId { get; set; }
        public Project Project { get; set; }
    }
}
