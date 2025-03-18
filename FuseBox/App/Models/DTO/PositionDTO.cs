using FuseBox.App.Models.BaseAbstract;

namespace FuseBox.App.Models.DTO
{
    public class PositionDTO : BaseEntity
    {
        public int IndexStart { get; set; }
        public int IndexFinish { get; set; }

        public PositionDTO() { }

        public PositionDTO(int indexStart, int indexFinish)
        {
            IndexStart = indexStart;
            IndexFinish = indexFinish;
        }


        // public override string ToString() => $"List №{ListIndex}, Object №{ObjectIndex}";
    }
}
