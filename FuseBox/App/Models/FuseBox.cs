using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FuseBox
{
    public class FuseBox : IPricedComponent, IHasConsumer
    {
        [Required(ErrorMessage = "Required field")]
        public bool MainBreaker { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool Main3PN { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool SurgeProtection { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool LoadSwitch2P { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool ModularContactor { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool RailMeter { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool FireUZO { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool VoltageRelay { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool ThreePRelay { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool RailSocket { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool NDiscLine { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool LoadSwitch { get; set; }

        [Required(ErrorMessage = "Required field")]
        public bool CrossModule { get; set; }

        public int DINLines { get; set; }

        public decimal Price { get; set; } // $

        // Список не отключаемых устройств
        public List<BaseElectrical> CriticalLine { get; set; } = new(); // Нужно добавить устройства с фронтэнд-части

        // Список подключенных к контактору устройств
        public List<BaseElectrical> Contactor { get; set; } = new(); // Нужно добавить устройства с фронтэнд-части
        
        public List<List<BaseElectrical>> Components { get; set; } = new List<List<BaseElectrical>> // Итоговый список устройств. Создана первая строка для работы логики комплектования щитовой
        {
            new List<BaseElectrical>(),
        }; 
        public List<BaseElectrical> Electricals { get; set; } = new(); // Базовый список 

        public List<Connection> CableConnections { get; set; } = new(); // Лучше перенести это поле в Components
    }
}
