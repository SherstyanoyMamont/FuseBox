namespace FuseBox.App.Contracts
{
    public sealed class FloorGroupingRequest
    {
        public bool IndividualFloorGrouping { get; set; }
        public bool SeparateRcdPerFloor { get; set; }
    }
}
