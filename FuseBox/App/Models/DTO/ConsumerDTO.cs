using FuseBox.App.Interfaces;
using System.ComponentModel.DataAnnotations;
using FuseBox.App.Interfaces;

namespace FuseBox.App.Models.DTO
{
    public class ConsumerDTO : ComponentDTO, IZone
    {
        [Required(ErrorMessage = "Укажите мощность потребителя в ваттах.")]
        [Range(0, double.MaxValue,
            ErrorMessage = "Мощность не может быть отрицательной.")]
        public double? PowerWatts { get; set; }

        [AllowedValues(
            2, 4, 6, 10, 16, 20, 25, 32,
            ErrorMessage = "Недопустимый номинал автомата.")]
        public int BreakerAmperage { get; set; } = 16;

        [AllowedValues(
            10, 30,
            ErrorMessage = "Чувствительность RCD должна быть 10 или 30 мА.")]
        public int RcdMilliAmps { get; set; } = 30;
    }
}