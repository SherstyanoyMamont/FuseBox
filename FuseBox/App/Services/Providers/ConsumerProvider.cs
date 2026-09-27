using FuseBox.App.Interfaces;
using System.Collections.Generic;

namespace FuseBox.App.Services.Providers
{
    public class ConsumerProvider : IConsumerProvider
    {
        private readonly Project project;

        public ConsumerProvider(Project project)
        {
            this.project = project;
        }

        public List<Consumer> GetAllConsumers()
        {
            var consumers = new List<Consumer>();
            foreach (var floor in project.Floors)
            {
                foreach (var room in floor.Rooms)
                {
                    consumers.AddRange(room.Consumer);
                }
            }
            return consumers;
        }

        public int GetTotalNumberOfRooms()
        {
            return project.GetTotalNumberOfRooms();
        }
    }
}
