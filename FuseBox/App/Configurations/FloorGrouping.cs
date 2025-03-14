using FuseBox.App.Models.BaseAbstract;
using System.ComponentModel.DataAnnotations;

namespace FuseBox
{
    // Grouping by floors
    public class FloorGrouping : BaseEntity
    {
        [Required(ErrorMessage = "Required field")]
        public bool IndividualFloorGrouping { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool SeparateUZOPerFloor { get; set; }

        public FloorGrouping() { }

        public FloorGrouping(bool individualFloorGrouping, bool separeteUzoPerFloor)
        {
            IndividualFloorGrouping = individualFloorGrouping;
            SeparateUZOPerFloor = separeteUzoPerFloor;
        }
    }
}
