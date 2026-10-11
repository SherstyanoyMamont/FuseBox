using FuseBox.App.Models.Shild_Comp;
using FuseBox.App.Models;
using FuseBox.App.Interfaces;

namespace FuseBox.App.Factorys
{
    public class ConnectionFactory : IConnectionFactory
    {
        public Position CreatePositionModule(int indexStart, int indexFinish)
        {
            return new Position(indexStart, indexFinish);
        }

        public Cable CreateCableModule(string colour, decimal section)
        {
            return new Cable(colour, section);
        }
    }
}
