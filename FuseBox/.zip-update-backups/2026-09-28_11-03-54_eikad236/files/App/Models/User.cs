using FuseBox.App.Models.BaseAbstract;

namespace FuseBox.App.Models
{
    public class User : BaseEntity
    {
        public string? Email { get; set; }

        public List<Project>? Projects { get; set; }

        public User() { }
    }
}
