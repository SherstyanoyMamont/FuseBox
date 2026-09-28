using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts
{
    public sealed class ConsumerRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public double PowerWatts { get; set; }

        [AllowedValues(2, 4, 6, 10, 16, 20, 25, 32)]
        public int BreakerAmperage { get; set; } = 16;

        [AllowedValues(10, 30)]
        public int RcdMilliAmps { get; set; } = 30;
    }
}
