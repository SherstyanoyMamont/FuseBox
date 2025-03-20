using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;

namespace FuseBox.App.Models.DTO
{
    public class ConnectionDTO : BaseEntity
    {
        public CableDTO? Cable { get; set; }
        public PositionDTO? CabelWay { get; set; }

        public ConnectionDTO() { }


    }
}
