using FuseBox.App.Interfaces;
using FuseBox.App.Models.DTO;
using FuseBox.App.Models.DTO.ConfugurationDTO;

namespace FuseBox
{
    public class ContactorDTO : ComponentDTO
    {
        public List<ComponentDTO> Electricals { get; set; } = new List<ComponentDTO>();


        public ContactorDTO() { }
    }
}
