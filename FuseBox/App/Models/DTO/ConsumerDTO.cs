using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Models.DTO
{
    public class ConsumerDTO : ComponentDTO, IZone
    {
        [Required(ErrorMessage = "Enter consumer power in watts.")]
        [Range(0, double.MaxValue,
            ErrorMessage = "Power cannot be negative.")]
        public double? PowerWatts { get; set; }

        [StringLength(64)]
        public string? CatalogTypeId { get; set; }

        [Required]
        [RegularExpression(
            "^(estimated|manual)$",
            ErrorMessage = "PowerSource must be estimated or manual.")]
        public string PowerSource { get; set; } =
            ConsumerPowerSourceValues.Manual;

        [AllowedValues(
            2, 4, 6, 10, 16, 20, 25, 32,
            ErrorMessage = "Invalid breaker rating.")]
        public int BreakerAmperage { get; set; } = 16;

        [AllowedValues(
            10, 30,
            ErrorMessage = "RCD sensitivity must be 10 or 30 mA.")]
        public int RcdMilliAmps { get; set; } = 30;
    }
}
