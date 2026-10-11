using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FuseBox.App.Models
{
    public class CableConnection : BaseEntity
    {
        public Cable Cable { get; set; }
        public Position CabelWay { get; set; }

        public int FuseBoxUnitId { get; set; }
        [JsonIgnore]
        public FuseBoxUnit FuseBoxUnit { get; set; }

        [NotMapped]
        public Cable? TempCable { get; set; }

        public CableConnection(Cable cable, Position cabelWay)
        {
            Cable = cable;
            CabelWay = cabelWay;

        }

        public CableConnection() { }
    }
}
