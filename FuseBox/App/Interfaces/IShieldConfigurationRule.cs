using FuseBox.App.Services.Providers;

namespace FuseBox.App.Interfaces
{
    public interface IShieldConfigurationRule
    {
        bool ShouldApply(IProjectSettings settings);
        Component CreateComponent(IComponentFactory factory);
    }
}
