namespace FuseBox.App.Interfaces
{
    public interface IProjectSettings
    {
        // InitialSettings
        int GetPhasesCount();
        int GetMainAmperage();
        int GetShieldWidth();
        int GetVoltageStandard();
        int GetPowerCoefficient();

        // 1P
        bool IsIntroductoryEnabled();
        bool IsSurgeProtectionEnabled();
        bool IsLoadSwitch2PEnabled();
        bool IsRailMeterEnabled();
        bool IsFireUZOEnabled();
        bool IsVoltageRelayEnabled();
        bool IsRailSocketEnabled();
        bool IsNDiscLineEnabled();
        bool IsLoadSwitchEnabled();
        bool IsModularContactorEnabled();
        bool IsCrossModuleEnabled();

        // 3P
        bool IsIntroductory3pEnabled();
        bool IsIntroductory3pnEnabled();
        bool IsSPD3Enabled();
        bool IsDinRailMeter3pEnabled();
        bool IsRCDFireEnabled();
        bool IsVoltageRelay1Enabled();
        bool IsVoltageRelay2Enabled();
        bool IsVoltageRelay3Enabled();
        bool IsDinRailSocketEnabled();
        bool IsModularContactor3pEnabled();
        bool IsCrossBlockEnabled();
    }
}
