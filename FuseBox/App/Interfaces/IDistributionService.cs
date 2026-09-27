namespace FuseBox.App.Interfaces
{
    public interface IDistributionService
    {
        void DistributeOfConsumers();
        void DistributeRCDFromLoad();
        List<RCD> GetDistributedRCDModules(); // например, вернет список УЗО
    }
}
