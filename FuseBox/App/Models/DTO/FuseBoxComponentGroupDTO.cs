using FuseBox.App.Models.BaseAbstract;

namespace FuseBox.App.Models.DTO
{
    public class FuseBoxComponentGroupDTO : BaseEntity
    {
        // Компоненты в группе
        public List<ComponentDTO> Components { get; set; } = new();

    }
}
