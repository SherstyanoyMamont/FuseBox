using FuseBox.App.Interfaces;
using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.DTO.ConfugurationDTO;
using Newtonsoft.Json;

namespace FuseBox.App.Models.DTO
{
    public class ComponentDTO : BaseElectrical, IPricedComponent // Abstract?
    {
        // public static int _idCounter = 0; // Static counter for all objects of this class
        public decimal Price { get; set; }

        // !!! Скрытые списки разьемов
        [JsonIgnore]
        [JsonProperty(Order = 8)]

        public List<PortDTO> Ports = new List<PortDTO>();

        [JsonProperty(Order = 5)]
        public int Slots { get; set; }

        public ComponentDTO(string name, int amper, int slots, decimal price, List<PortDTO> ports)
        {
            Ports = ports;
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
        public ComponentDTO(string name, int amper, int slots, decimal price)
        {
            Name = name;
            Amper = amper;
            Slots = slots;
            Price = price;
        }
        public ComponentDTO(string name, int slot)     // Для тестов
        {
            Name = name;
            Slots = slot;
        }
        public ComponentDTO() { }      // Для тестов
    }
}
