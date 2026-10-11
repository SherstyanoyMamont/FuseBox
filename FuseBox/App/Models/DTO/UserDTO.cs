using FuseBox.App.Models.BaseAbstract;

namespace FuseBox.App.Models.DTO
{
    public class UserDTO : BaseEntity
    {
        public string Email { get; set; }

        public List<ProjectDTO> Projects { get; set; }

        public UserDTO() { }
    }
}
