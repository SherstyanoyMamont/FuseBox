using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;

namespace FuseBox
{
    public class Consumer : Component, IZone
    {
        // Связь с комнатой
        public int RoomId { get; set; }

        public Consumer() { }
        public Consumer(string name, int maxLoad)        // Для тестов
        {
            this.Name = name;
            this.Amper = maxLoad;
        }
    }
}
