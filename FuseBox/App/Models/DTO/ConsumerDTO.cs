using FuseBox.App.Interfaces;
using System.Text.Json.Serialization;

namespace FuseBox.App.Models.DTO
{
    public class ConsumerDTO : ComponentDTO, IZone
    {

        public ConsumerDTO() { }
        public ConsumerDTO(string name, int maxLoad)        // Для тестов
        {
            Name = name;
            Amper = maxLoad;
        }
    }
}
