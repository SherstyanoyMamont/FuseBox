using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FuseBox.App.Models.DTO
{
    public class FuseBoxUnitDTO : BaseEntity, IPricedComponent
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



        // Список подключенных к контактору устройств
        //public List<Consumer> Contactor { get; set; } = new(); // Нужно добавить устройства с фронтэнд-части

        public List<FuseBoxComponentGroupDTO>? ComponentGroups { get; set; } // Итоговый список устройств. Создана первая строка для работы логики комплектования щитовой


        //[NotMapped]
        // Список не отключаемых устройств
        //public List<Component> CriticalLine { get; set; } = new(); // Нужно добавить устройства с фронтэнд-части

        //public List<Component> Electricals { get; set; } = new(); // Базовый список 

        public List<ConnectionDTO> CableConnections { get; set; } = new(); // Лучше перенести это поле в Components


        public FuseBoxUnitDTO() { }


    }
}
