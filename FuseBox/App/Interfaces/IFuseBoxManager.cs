using FuseBox.App.Models;
using FuseBox.App.Models.Shild_Comp;
using System.ComponentModel;

namespace FuseBox.App.Interfaces
{
    public interface IFuseBoxManager
    {
        void AddComponentGroup();
        void AddConnection(Cable cable, Position position);
        void AddComponentOnLevel(int currentLevel, Component component);
        void BindComponentsToGroups();
        void ClearConnections();
        List<FuseBoxComponentGroup> GetComponentGroups();
    }
}
