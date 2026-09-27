using FuseBox.App.Interfaces;
using FuseBox.App.Models.Shild_Comp;
using FuseBox.App.Models;

namespace FuseBox.App.Services.Providers
{
    public class FuseBoxCableConnectionManager : IFuseBoxManager
    {

        private readonly FuseBoxUnit fuseBox;

        public FuseBoxCableConnectionManager(FuseBoxUnit fuseBox)
        {
            this.fuseBox = fuseBox;
        }

        public void AddComponentGroup()
        {
            fuseBox.ComponentGroups.Add(new FuseBoxComponentGroup());
        }

        public void AddConnection(Cable cable, Position position)
        {
            fuseBox.CableConnections.Add(new CableConnection(cable, position));
        }

        //public void AddComponent(Component component)
        //{
        //    fuseBox.ComponentGroups.Add(component);
        //}

        public void AddComponentOnLevel(int currentLevel, Component component)
        {
            fuseBox.ComponentGroups[currentLevel].Components.Add(component);
        }


        public void BindComponentsToGroups()
        {
            fuseBox.BindComponentsToGroups();
        }

        public List<FuseBoxComponentGroup> GetComponentGroups()
        {
            return fuseBox.ComponentGroups;
        }

        public void ClearConnections()
        {
            fuseBox.CableConnections.Clear();
        }
    }
}
