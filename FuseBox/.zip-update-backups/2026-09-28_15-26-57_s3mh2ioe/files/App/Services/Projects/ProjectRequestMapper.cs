using FuseBox.App.Contracts.Projects;

namespace FuseBox.App.Services.Projects
{
    public static class ProjectRequestMapper
    {
        public static Project ToProject(ProjectSaveRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!IsWholeNumber(
                    request.InitialSettings.PowerCoefficient))
            {
                throw new ArgumentException(
                    "PowerCoefficient must currently be a whole " +
                    "number (0, 1 or 2).");
            }

            var project = new Project
            {
                Name = NormalizeRequiredName(
                    request.Name,
                    "Project"),

                FloorGrouping = new FloorGrouping
                {
                    IndividualFloorGrouping =
                        request.FloorGrouping
                            .IndividualFloorGrouping,

                    SeparateUZOPerFloor =
                        request.FloorGrouping
                            .SeparateRcdPerFloor
                },

                GlobalGrouping = new GlobalGrouping
                {
                    Sockets = request.GlobalGrouping.Sockets,
                    Lighting = request.GlobalGrouping.Lighting,
                    Conditioners =
                        request.GlobalGrouping.Conditioners
                },

                InitialSettings =
                    new global::FuseBox.FuseBox.InitialSettings
                    {
                        PhasesCount =
                            request.InitialSettings.PhasesCount,
                        MainAmperage =
                            request.InitialSettings.MainAmperage,
                        ShieldWidth =
                            request.InitialSettings.ShieldWidth,
                        VoltageStandard =
                            request.InitialSettings.VoltageStandard,
                        PowerCoefficient =
                            checked((int)
                                request.InitialSettings
                                    .PowerCoefficient)
                    },

                FuseBox = new FuseBoxUnit
                {
                    MainBreaker = request.FuseBox.MainBreaker,
                    Main3PN = request.FuseBox.Main3PN,
                    SurgeProtection =
                        request.FuseBox.SurgeProtection,
                    LoadSwitch2P =
                        request.FuseBox.LoadSwitch2P,
                    ModularContactor =
                        request.FuseBox.ModularContactor,
                    RailMeter = request.FuseBox.RailMeter,
                    FireUZO = request.FuseBox.FireUZO,
                    VoltageRelay =
                        request.FuseBox.VoltageRelay,
                    ThreePRelay =
                        request.FuseBox.ThreePRelay,
                    RailSocket = request.FuseBox.RailSocket,

                    NDiscLine =
                        request.FuseBox
                            .NDisconnectableLine,

                    LoadSwitch = request.FuseBox.LoadSwitch,
                    CrossModule = request.FuseBox.CrossModule,
                    DINLines = request.FuseBox.DINLines,
                    Price = request.FuseBox.Price
                }
            };

            project.Floors = request.Floors
                .Select(floorRequest =>
                {
                    var floor = new Floor
                    {
                        Name = NormalizeRequiredName(
                            floorRequest.Name,
                            "Floor")
                    };

                    floor.Rooms = floorRequest.Rooms
                        .Select(roomRequest =>
                        {
                            var room = new Room
                            {
                                Name =
                                    NormalizeRequiredName(
                                        roomRequest.Name,
                                        "Room")
                            };

                            room.Consumer =
                                roomRequest.Consumers
                                    .Select(
                                        consumerRequest =>
                                            new Consumer
                                            {
                                                Name =
                                                    NormalizeRequiredName(
                                                        consumerRequest
                                                            .Name,
                                                        "Consumer"),

                                                PowerWatts =
                                                    consumerRequest
                                                        .PowerWatts,

                                                BreakerAmperage =
                                                    consumerRequest
                                                        .BreakerAmperage,

                                                RcdMilliAmps =
                                                    consumerRequest
                                                        .RcdMilliAmps
                                            })
                                    .ToList();

                            return room;
                        })
                        .ToList();

                    return floor;
                })
                .ToList();

            return project;
        }

        private static bool IsWholeNumber(double value)
        {
            return double.IsFinite(value) &&
                   Math.Abs(value - Math.Round(value)) <
                   0.0000001;
        }

        private static string NormalizeRequiredName(
            string? value,
            string entityName)
        {
            var normalized = value?.Trim();

            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException(
                    $"{entityName} name is required.");
            }

            return normalized.Length <= 120
                ? normalized
                : normalized[..120];
        }
    }
}
