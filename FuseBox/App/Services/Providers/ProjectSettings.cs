using FuseBox.App.Interfaces;

namespace FuseBox.App.Services.Providers
{
    public class ProjectSettings : IProjectSettings
    {
        private readonly Project project;

        public ProjectSettings(Project project)
        {
            this.project = project;
        }

        // InitialSettings
        public int GetPhasesCount() => project.InitialSettings.PhasesCount;
        public int GetMainAmperage() => project.InitialSettings.MainAmperage;
        public int GetShieldWidth() => project.InitialSettings.ShieldWidth;
        public int GetVoltageStandard() => project.InitialSettings.VoltageStandard;
        public int GetPowerCoefficient() => project.InitialSettings.PowerCoefficient;


        // 1P
        public bool IsIntroductoryEnabled() => project.FuseBox.MainBreaker;
        public bool IsSurgeProtectionEnabled() => project.FuseBox.SurgeProtection;
        public bool IsLoadSwitch2PEnabled() => project.FuseBox.LoadSwitch2P;
        public bool IsRailMeterEnabled() => project.FuseBox.RailMeter;
        public bool IsFireUZOEnabled() => project.FuseBox.FireUZO;
        public bool IsVoltageRelayEnabled() => project.FuseBox.VoltageRelay;
        public bool IsRailSocketEnabled() => project.FuseBox.RailSocket;
        public bool IsNDiscLineEnabled() => project.FuseBox.NDiscLine;
        public bool IsLoadSwitchEnabled() => project.FuseBox.LoadSwitch;
        public bool IsModularContactorEnabled() => project.FuseBox.ModularContactor;
        public bool IsCrossModuleEnabled() => project.FuseBox.CrossModule;

        // 3P
        public bool IsIntroductory3pEnabled() => project.FuseBox.MainBreaker;
        public bool IsIntroductory3pnEnabled() => project.FuseBox.Main3PN;
        public bool IsSPD3Enabled() => project.FuseBox.SurgeProtection;
        public bool IsDinRailMeter3pEnabled() => project.FuseBox.RailMeter;
        public bool IsRCDFireEnabled() => project.FuseBox.FireUZO;
        public bool IsVoltageRelay1Enabled() => project.FuseBox.VoltageRelay;
        public bool IsVoltageRelay2Enabled() => project.FuseBox.VoltageRelay;
        public bool IsVoltageRelay3Enabled() => project.FuseBox.VoltageRelay;
        public bool IsDinRailSocketEnabled() => project.FuseBox.RailSocket;
        public bool IsModularContactor3pEnabled() => project.FuseBox.ModularContactor;
        public bool IsCrossBlockEnabled() => project.FuseBox.CrossModule;
    }
}
