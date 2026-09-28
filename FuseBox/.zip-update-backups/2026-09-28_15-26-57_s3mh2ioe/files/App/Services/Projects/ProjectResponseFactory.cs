using FuseBox.App.Models.Shild_Comp;
using Newtonsoft.Json;

namespace FuseBox.App.Services.Projects
{
    public static class ProjectResponseFactory
    {
        public static string BuildSchemaJson(Project project)
        {
            if (project.FuseBox == null)
            {
                throw new InvalidOperationException(
                    "Project fuse box is missing.");
            }

            var fuseBox = project.FuseBox;

            var devices = fuseBox.ComponentGroups
                .SelectMany(group => group.Components)
                .Where(component =>
                    component is not EmptySlot)
                .ToList();

            if (devices.Any(component =>
                component.SerialNumber <= 0))
            {
                throw new InvalidOperationException(
                    "A schema device has a non-positive SchemaId.");
            }

            if (devices
                .GroupBy(component => component.SerialNumber)
                .Any(group => group.Count() > 1))
            {
                throw new InvalidOperationException(
                    "Duplicate SchemaId values were found.");
            }

            var schemaIds = devices
                .Select(component => component.SerialNumber)
                .ToHashSet();

            var hasInvalidConnection =
                fuseBox.CableConnections.Any(connection =>
                    connection.CabelWay == null ||
                    !schemaIds.Contains(
                        connection.CabelWay.IndexStart) ||
                    !schemaIds.Contains(
                        connection.CabelWay.IndexFinish));

            if (hasInvalidConnection)
            {
                throw new InvalidOperationException(
                    "A schema connection points to a missing device.");
            }

            var incomingConnections =
                fuseBox.CableConnections
                    .ToLookup(connection =>
                        connection.CabelWay.IndexFinish);

            var rcdIds = devices
                .OfType<RCD>()
                .Select(rcd => rcd.SerialNumber)
                .ToHashSet();

            string? GetPhaseColour(Component component)
            {
                if (component is not RCD &&
                    component is not Fuse)
                {
                    return null;
                }

                var colours =
                    incomingConnections[
                            component.SerialNumber]
                        .Select(connection =>
                            connection.Cable?.Сolour)
                        .Where(colour =>
                            colour == "Red" ||
                            colour == "Orange" ||
                            colour == "Grey")
                        .Distinct()
                        .ToList();

                return colours.Count == 1
                    ? colours[0]
                    : null;
            }

            int? GetParentRcdId(Component component)
            {
                if (component is not Fuse)
                    return null;

                var parentIds =
                    incomingConnections[
                            component.SerialNumber]
                        .Select(connection =>
                            connection.CabelWay.IndexStart)
                        .Where(sourceId =>
                            rcdIds.Contains(sourceId))
                        .Distinct()
                        .ToList();

                return parentIds.Count == 1
                    ? parentIds[0]
                    : null;
            }

            var result = new
            {
                ComponentGroups = fuseBox.ComponentGroups
                    .OrderBy(group => group.Components
                        .Where(component =>
                            component is not EmptySlot)
                        .Select(component =>
                            component.SerialNumber)
                        .DefaultIfEmpty(int.MaxValue)
                        .Min())
                    .Select(group => new
                    {
                        Components = group.Components
                            .OrderBy(component =>
                                component is EmptySlot
                                    ? 1
                                    : 0)
                            .ThenBy(component =>
                                component.SerialNumber)
                            .Select(component => new
                            {
                                SchemaId =
                                    component is EmptySlot
                                        ? (int?)null
                                        : component.SerialNumber,

                                DatabaseId = component.Id,

                                Name = component.Name?
                                    .Replace(" ", ""),

                                Slots = component.Slots,
                                Amper = component.Amper,

                                Capacity =
                                    component is RCD rcd
                                        ? (int?)rcd.Capacity
                                        : null,

                                PhaseColour =
                                    GetPhaseColour(component),

                                ParentRcdSchemaId =
                                    GetParentRcdId(component),

                                Electricals =
                                    component is Fuse fuse
                                        ? fuse.Electricals
                                        : null
                            })
                            .ToList()
                    })
                    .ToList(),

                CableConnections = fuseBox.CableConnections
                    .Select(connection => new
                    {
                        Cable =
                            connection.Cable == null
                                ? null
                                : new
                                {
                                    Colour =
                                        connection.Cable.Сolour
                                },

                        CabelWay = new
                        {
                            IndexStart =
                                connection.CabelWay.IndexStart,

                            IndexFinish =
                                connection.CabelWay.IndexFinish
                        }
                    })
                    .ToList()
            };

            return JsonConvert.SerializeObject(
                result,
                Formatting.Indented);
        }

        public static string BuildConfigurationJson(
            Project project)
        {
            if (project.FuseBox == null ||
                project.InitialSettings == null)
            {
                throw new InvalidOperationException(
                    "Project configuration is incomplete.");
            }

            var settings = project.InitialSettings;
            var devices = project.FuseBox;

            var result = new
            {
                version = 1,
                projectId = project.Id,
                projectName = project.Name,

                primarySettings = new
                {
                    settings.PhasesCount,
                    settings.MainAmperage,
                    settings.ShieldWidth,
                    settings.VoltageStandard,
                    settings.PowerCoefficient
                },

                shieldDevices = new
                {
                    devices.MainBreaker,
                    devices.Main3PN,
                    devices.SurgeProtection,
                    devices.LoadSwitch2P,
                    devices.ModularContactor,
                    devices.RailMeter,
                    devices.FireUZO,
                    devices.VoltageRelay,
                    devices.RailSocket,

                    NDisconnectableLine =
                        devices.NDiscLine,

                    devices.LoadSwitch,
                    devices.CrossModule,
                    devices.DINLines,
                    devices.Price
                },

                floors = project.Floors
                    .OrderBy(floor => floor.Id)
                    .Select(floor => new
                    {
                        id = $"floor-{floor.Id}",
                        name = floor.Name,

                        rooms = floor.Rooms
                            .OrderBy(room => room.Id)
                            .Select(room => new
                            {
                                id = $"room-{room.Id}",
                                name = room.Name,

                                consumers =
                                    room.Consumer
                                        .OrderBy(
                                            consumer =>
                                                consumer.Id)
                                        .Select(
                                            consumer =>
                                                new
                                                {
                                                    icon = "⚡",
                                                    name =
                                                        consumer.Name,
                                                    ab =
                                                        $"C{consumer.BreakerAmperage}",
                                                    watt =
                                                        consumer
                                                            .PowerWatts!
                                                            .Value,
                                                    rcdMilliAmps =
                                                        consumer
                                                            .RcdMilliAmps
                                                })
                                        .ToList()
                            })
                            .ToList()
                    })
                    .ToList()
            };

            return JsonConvert.SerializeObject(result);
        }

        public static bool HasUnknownPower(Project project)
        {
            return project.Floors
                .SelectMany(floor => floor.Rooms)
                .SelectMany(room => room.Consumer)
                .Any(consumer =>
                    !consumer.PowerWatts.HasValue);
        }
    }
}
