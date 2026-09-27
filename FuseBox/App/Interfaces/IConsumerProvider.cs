namespace FuseBox.App.Interfaces
{
    public interface IConsumerProvider
    {
        List<Consumer> GetAllConsumers();

        int GetTotalNumberOfRooms();
    }
}
