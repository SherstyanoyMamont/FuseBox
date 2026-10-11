using FuseBox.App.Interfaces;

namespace FuseBox.App.Services.Providers
{
    public class ProjectGroupingProvider : IProjectGrouping
    {
        private readonly Project project;

        public ProjectGroupingProvider(Project project)
        {
            this.project = project;
        }

        public int GetSocketsGrouping()
        {
            return project.GlobalGrouping.Sockets;
        }
        public int GetLightingsGrouping()
        {
            return project.GlobalGrouping.Lighting;
        }
        public int GetConditionersGrouping()
        {
            return project.GlobalGrouping.Conditioners;
        }
        public bool IsIndividualFloorGroupingEnable()
        {
            return project.FloorGrouping.IndividualFloorGrouping;
        }
        public bool IsSeparateUZOPerFloorEnable()
        {
            return project.FloorGrouping.SeparateUZOPerFloor;
        }
    }
}
