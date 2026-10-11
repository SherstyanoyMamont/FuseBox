using FuseBox.App.Models.BaseAbstract;
using FuseBox.App.Models.DTO.ConfugurationDTO;
using FuseBox.FuseBox;

namespace FuseBox.App.Models.DTO
{
    public class ProjectDTO : BaseEntity
    {
        public FloorGroupingDTO? FloorGrouping { get; set; }
        public GlobalGroupingDTO? GlobalGrouping { get; set; }
        public InitialSettingsDTO? InitialSettings { get; set; }
        public FuseBoxUnitDTO? FuseBox { get; set; }
        public List<FloorDTO> Floors { get; set; } = new();

        // Persisted project total is power in watts.
        public double TotalPower { get; set; }

        public double CalculateTotalCurrentAmps()
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Consumer)
                .Sum(consumer => consumer.Amper);
        }

        public double? CalculateTotalPowerWatts()
        {
            var consumers = Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Consumer)
                .ToList();

            if (consumers.Any(consumer =>
                    !consumer.PowerWatts.HasValue))
            {
                return null;
            }

            return consumers.Sum(consumer =>
                consumer.PowerWatts!.Value);
        }

        [Obsolete(
            "CalculateTotalPower historically returned current in amps. " +
            "Use CalculateTotalCurrentAmps or CalculateTotalPowerWatts.")]
        public double CalculateTotalPower()
        {
            return CalculateTotalCurrentAmps();
        }

        public int GetTotalNumberOfRooms()
        {
            return Floors
                .SelectMany(floor => floor.Rooms)
                .Count();
        }
    }
}
