using FuseBox.App.Models;
using FuseBox.App.Models.Shild_Comp;

namespace FuseBox.App.Interfaces
{
    public interface IConnectionFactory
    {
        Position CreatePositionModule(int indexStart, int indexFinish);
        Cable CreateCableModule(string colour, decimal section);
    }
}
