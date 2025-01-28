using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using System.Collections.Generic;

namespace FuseBox
{
    public class FuseBox : IPricedComponent, IHasConsumer
    {
        public bool MainBreaker { get; set; }
        public bool Main3PN { get; set; }
        public bool SurgeProtection { get; set; }
        public bool LoadSwitch2P { get; set; }
        public bool ModularContactor { get; set; }
        public bool RailMeter { get; set; }
        public bool FireUZO { get; set; }
        public bool VoltageRelay { get; set; }
        public bool ThreePRelay { get; set; }
        public bool RailSocket { get; set; }
        public bool NDisconnectableLine { get; set; }
        public bool LoadSwitch { get; set; }
        public bool CrossModule { get; set; }
        public int DINLines { get; set; }
        public decimal Price { get; set; } // $

        // Список не отключаемых устройств
        public List<BaseElectrical> CriticalLine { get; set; } = new(); // Нужно добавить устройства с фронтэнд-части

        // Список подключенных к контактору устройств
        public List<BaseElectrical> Contactor { get; set; } = new(); // Нужно добавить устройства с фронтэнд-части
        
        public List<List<BaseElectrical>> Components { get; set; } = new(); // Итоговый список устройств
        public List<BaseElectrical> Electricals { get; set; } = new(); // Базовый список 
    }
}
