using FuseBox.App.Models.BaseAbstract;
using System.ComponentModel.DataAnnotations;

namespace FuseBox
{
    // Grouping lines (default: "By rooms")
    public class GlobalGrouping : BaseEntity
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

        // Связь с проектом
        public int ProjectId { get; set; }
        public Project Project { get; set; }
        public GlobalGrouping(int sockets, int lighting, int conditioners)
        {
            this.Sockets = sockets;
            this.Lighting = lighting;
            this.Conditioners = conditioners;
        }
        public GlobalGrouping() { }
    }
}
