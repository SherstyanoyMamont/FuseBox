using FuseBox.App.Interfaces;
using FuseBox.App.Models.DTO;
using FuseBox.App.Models.DTO.ConfugurationDTO;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;

namespace FuseBox
{
    public class RCDDTO : ComponentDTO
    {
        public List<ComponentDTO> Electricals { get; set; } = new List<ComponentDTO>();

        [JsonProperty(Order = 8)]
        public int Capacity { get; set; }

        public const decimal LimitOfConnectedFuses = 5;

        [JsonProperty(Order = 4)]
        public double TotalLoad { get; set; } // Added in DistributeFusesToRCDs


        public RCDDTO() { }
    }
}
