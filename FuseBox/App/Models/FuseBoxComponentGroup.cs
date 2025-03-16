using FuseBox.App.Models.BaseAbstract;

namespace FuseBox
{
    public class FuseBoxComponentGroup : BaseEntity
    {
        // Компоненты в группе
        public List<BaseElectrical> Components { get; set; } = new();

        // Связь с FuseBox
        public int FuseBoxUnit5 { get; set; }
        public FuseBoxUnit FuseBox { get; set; }

    }
}
