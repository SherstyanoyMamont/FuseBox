namespace FuseBox
{
    // Grouping lines (default: "By rooms")
    public class GlobalGroupingParameters
    {
        public int Sockets { get; set; }
        public int Lighting { get; set; }
        public int Conditioners { get; set; }
    }

    // Grouping by floors
    public class FloorGrouping
    {
        public bool IndividualFloorGrouping { get; set; }
        public bool SeparateUZOPerFloor { get; set; }
    }
}
