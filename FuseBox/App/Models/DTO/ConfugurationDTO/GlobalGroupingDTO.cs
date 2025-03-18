using FuseBox.App.Models.BaseAbstract;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FuseBox.App.Models.DTO.ConfugurationDTO
{
    // Grouping lines (default: "By rooms")
    public class GlobalGroupingDTO : BaseEntity
    {
        [Required(ErrorMessage = "Required field")]
        [Range(0, 5, ErrorMessage = "Sockets from 0 to 5")]
        public int Sockets { get; set; }

        [Required(ErrorMessage = "Required field")]
        [Range(0, 5, ErrorMessage = "Lighting from 0 to 5")]
        public int Lighting { get; set; }

        [Required(ErrorMessage = "Required field")]
        [Range(0, 5, ErrorMessage = "Conditioners from 0 to 5")]
        public int Conditioners { get; set; }

        public GlobalGroupingDTO(int sockets, int lighting, int conditioners)
        {
            Sockets = sockets;
            Lighting = lighting;
            Conditioners = conditioners;
        }
        public GlobalGroupingDTO() { }
    }
}
