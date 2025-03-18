using FuseBox.App.Models.BaseAbstract;
using System.Text.Json.Serialization;

namespace FuseBox.App.Models
{
    public class Position : BaseEntity
    {
        public int IndexStart { get; set; }
        public int IndexFinish { get; set; }

        // Связь с Connection
        public int ConnectionPositionId { get; set; }
        [JsonIgnore]
        public Connection? Connection { get; set; }

        public Position() { }

        public Position(int indexStart, int indexFinish)
        {
            IndexStart = indexStart;
            IndexFinish = indexFinish;
        }


        // public override string ToString() => $"List №{ListIndex}, Object №{ObjectIndex}";
    }
}
