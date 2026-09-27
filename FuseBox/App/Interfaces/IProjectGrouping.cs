namespace FuseBox.App.Interfaces
{
    public interface IProjectGrouping
    {
        int GetSocketsGrouping();
        int GetLightingsGrouping();
        int GetConditionersGrouping();

        bool IsIndividualFloorGroupingEnable();
        bool IsSeparateUZOPerFloorEnable();

    }
}
