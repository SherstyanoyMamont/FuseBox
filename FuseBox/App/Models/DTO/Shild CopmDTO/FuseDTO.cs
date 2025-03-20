using FuseBox.App.Models.DTO;
using FuseBox.App.Models.DTO.ConfugurationDTO;
using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;

namespace FuseBox
{
    public class FuseDTO : ComponentDTO
    {
        [JsonProperty(Order = 8)]
        public List<ConsumerDTO> Electricals { get; set; } = new List<ConsumerDTO>();

        public FuseDTO() { }

        public double GetTotalLoad()
        {
            double totalLoad = 0;

            foreach (var item in Electricals)
            {
                totalLoad += item.Amper;
            }
            return totalLoad;
        }
    }
}
