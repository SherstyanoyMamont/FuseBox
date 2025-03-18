using FuseBox.App.Models.BaseAbstract;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FuseBox.App.Models.DTO.ConfugurationDTO
{
    // Grouping by floors
    public class FloorGroupingDTO : BaseEntity
    {
        [Required(ErrorMessage = "Required field")]
        public bool IndividualFloorGrouping { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool SeparateUZOPerFloor { get; set; }

        public FloorGroupingDTO() { }

        public FloorGroupingDTO(bool individualFloorGrouping, bool separeteUzoPerFloor)
        {
            IndividualFloorGrouping = individualFloorGrouping;
            SeparateUZOPerFloor = separeteUzoPerFloor;
        }
    }
}
