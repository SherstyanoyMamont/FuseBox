using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using FuseBox.App.Models.BaseAbstract;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FuseBox
{
    public class FuseBoxUnit : BaseEntity, IPricedComponent
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



        //// Список подключенных к контактору устройств
        //public List<Consumer> Contactor { get; set; } = new(); // Нужно добавить устройства с фронтэнд-части

        // Это надо оптимизировать
        public List<FuseBoxComponentGroup> ComponentGroups { get; set; } = new() // Итоговый список устройств. Создана первая строка для работы логики комплектования щитовой
        {
            new FuseBoxComponentGroup(),
        };


        //[NotMapped]
        //Список не отключаемых устройств
        //public List<Component> CriticalLine { get; set; } = new(); // Нужно добавить устройства с фронтэнд-части

        //public List<Component> Electricals { get; set; } = new(); // Базовый список 

        public List<Connection> CableConnections { get; set; } = new(); // Лучше перенести это поле в Components


        // Связь с проектом
        public int ProjectId { get; set; }
        [JsonIgnore]
        public Project? Project { get; set; }


        public FuseBoxUnit() { }

        public FuseBoxUnit(bool mainBreaker, bool main3PN, bool surgeProtection, bool loadSwitch2P,
                       bool modularContactor, bool railMeter, bool fireUzo, bool voltageRelay,
                       bool threePRelay, bool railSocket, bool nDiscLine, bool loadSwitch, bool crossModule)
        {

            MainBreaker = mainBreaker;
            Main3PN = main3PN;
            SurgeProtection = surgeProtection;
            LoadSwitch2P = loadSwitch2P;
            ModularContactor = modularContactor;
            RailMeter = railMeter;
            FireUZO = fireUzo;
            VoltageRelay = voltageRelay;
            ThreePRelay = threePRelay;
            RailSocket = railSocket;
            NDiscLine = nDiscLine;
            LoadSwitch = loadSwitch;
            CrossModule = crossModule;

        }
    }
}
