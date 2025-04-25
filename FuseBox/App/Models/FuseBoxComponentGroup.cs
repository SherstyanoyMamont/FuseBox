using FuseBox.App.Models.BaseAbstract;
using System.Text.Json.Serialization;

namespace FuseBox
{
    public class FuseBoxComponentGroup : BaseEntity
    {
        // Компоненты в группе
        public List<Component> Components { get; set; } = new(); // !!!

        // Связь с FuseBox
        public int FuseBoxUnitId { get; set; }
        [JsonIgnore]
        public FuseBoxUnit FuseBoxUnit { get; set; } = null!;

    }
}
