using FuseBox.App.Interfaces;

namespace FuseBox.App.Services.Providers
{
    public class IntroductoryModuleRule : IShieldConfigurationRule
    {
        public bool ShouldApply(IProjectSettings settings)
            => settings.IsIntroductoryEnabled();

        public Component CreateComponent(IComponentFactory factory)
            => factory.CreateIntroductoryModule();
    }
}
