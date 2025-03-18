using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.Shild_Comp;
using System.Text.Json.Serialization;

namespace FuseBox.App.Models
{
    public class Connection : BaseEntity
    {
        public Cable? Cable { get; set; }
        public Position? CabelWay { get; set; }

        public int FuseBoxUnitId { get; set; }
        [JsonIgnore]
        public FuseBoxUnit? FuseBoxUnit { get; set; }

        public Connection(Cable cable, Position cabelWay)
        {
            Cable = cable;
            CabelWay = cabelWay;

        }

        public Connection() { }
    }
}
