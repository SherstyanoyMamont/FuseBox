namespace FuseBox.App.Interfaces
{
    public interface IComponentFactory
    {
        // 1P
        Component CreateIntroductoryModule();
        Component CreateSurgeProtectionModule();
        Component CreateLoadSwitchModule();

        Component CreateRailMeterModule();
        Component CreateFireUZOModule();
        Component CreateVoltageRelayModule();
        Component CreateRailSocketModule();
        Component CreateNDiscLineModule();

        //Component CreateLoadSwitchModule();
        Component CreateModularContactorModule();
        Component CreateCrossBlockModule();
        

        // 3P
        Component CreateIntroductory3pModule();
        Component CreateIntroductory3pnModule();
        Component CreateSPD3Module();
        Component CreateDinRailMeter3pModule();
        Component CreateRCDFire3pModule();

        //Component CreateVoltageRelay1Module();
        Component CreateVoltageRelay2Module();
        Component CreateVoltageRelay3Module();
        Component CreateVoltageRelay3PModule();

        //Component CreateDinRailSocketModule();

        //Component CreateModularContactorModule();

        //Component CreateCrossBlock3pModule();
            
    }
}
