using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts.Projects
{
    public sealed class GlobalGroupingRequest
    {
        [Range(0, 5)]
        public int Sockets { get; set; }

        [Range(0, 5)]
        public int Lighting { get; set; }

        [Range(0, 5)]
        public int Conditioners { get; set; }
    }
}
