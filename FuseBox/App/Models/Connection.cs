

using FuseBox.App.Models;
using FuseBox.App.Models.Shild_Comp;

namespace FuseBox
{
    public class Connection : BaseEntity
    {
        public Cable Cable { get; set; }
        public Position CabelWay { get; set; }

        public Connection(Cable cable, Position cabelWay)
        {
            Cable = cable;
            CabelWay = cabelWay;

        }
    }
}
