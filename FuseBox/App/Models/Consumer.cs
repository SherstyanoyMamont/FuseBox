using System.ComponentModel.DataAnnotations;

namespace FuseBox
{
    public class Consumer : BaseEntity
    {
        [Required(ErrorMessage = "Power is required!")]
        [Range(0, 20000, ErrorMessage = "Power must be 0-20000.")]
        public int Watt { get; set; }
        // public bool Contactor { get; set; }   // Connected to contactor?
        public bool SeparateRCD { get; set; } // Separate RCD?
        public bool IsCritical { get; set; }  // Critical line (non-switchable)
        public bool Area { get; set; }        // "Dry" or "Wet"
    }
}
