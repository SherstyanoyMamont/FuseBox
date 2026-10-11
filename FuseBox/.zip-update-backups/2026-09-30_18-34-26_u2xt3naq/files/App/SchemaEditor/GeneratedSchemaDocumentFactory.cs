using System.Globalization;
using FuseBox.App.Models;
using FuseBox.App.Models.Shild_Comp;

namespace FuseBox.App.SchemaEditor;

/// <summary>
/// Converts the existing generator graph into the editor document contract
/// without mutating or renumbering generator entities.
/// </summary>
public static class GeneratedSchemaDocumentFactory
{
    public static SchemaDocument Create(Project project, int revision)
    {
        if (project.FuseBox == null)
            throw new InvalidOperationException("Project fuse box is missing.");

        if (revision <= 0)
            throw new ArgumentOutOfRangeException(nameof(revision));

        var fuseBox = project.FuseBox;
        var devices = fuseBox.ComponentGroups
            .SelectMany(group => group.Components)
            .Where(component => component is not EmptySlot)
            .ToList();

        if (devices.Any(component => component.SerialNumber <= 0))
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

        var bySchemaId = devices.ToDictionary(
            component => component.SerialNumber);

        foreach (var connection in fuseBox.CableConnections)
        {
            if (connection.CabelWay == null ||
                !bySchemaId.ContainsKey(connection.CabelWay.IndexStart) ||
                !bySchemaId.ContainsKey(connection.CabelWay.IndexFinish))
            {
                throw new InvalidOperationException(
                    "A schema connection points to a missing device.");
            }
        }

        var incomingConnections = fuseBox.CableConnections
            .ToLookup(connection => connection.CabelWay.IndexFinish);

        string? PhaseFor(Component component)
        {
            if (component is not RCD && component is not Fuse)
                return null;

            var phases = incomingConnections[component.SerialNumber]
                .Select(connection => ConductorFromColour(connection.Cable?.Сolour))
                .Where(conductor =>
                    conductor == "L1" ||
                    conductor == "L2" ||
                    conductor == "L3")
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return phases.Count == 1 ? phases[0] : null;
        }

        var componentModels = new Dictionary<int, EditorComponent>();
        var document = new SchemaDocument
        {
            Mode = SchemaModes.Generated,
            Revision = revision
        };

        var orderedGroups = fuseBox.ComponentGroups
            .OrderBy(group => group.Components
                .Where(component => component is not EmptySlot)
                .Select(component => component.SerialNumber)
                .DefaultIfEmpty(int.MaxValue)
                .Min())
            .ToList();

        for (var rowIndex = 0; rowIndex < orderedGroups.Count; rowIndex++)
        {
            var slotStart = 0;
            var orderedComponents = orderedGroups[rowIndex].Components
                .OrderBy(component => component is EmptySlot ? 1 : 0)
                .ThenBy(component => component.SerialNumber)
                .ToList();

            foreach (var component in orderedComponents)
            {
                var placement = new DinPlacement
                {
                    RowIndex = rowIndex,
                    SlotStart = slotStart,
                    Slots = component.Slots
                };

                slotStart += component.Slots;

                if (component is EmptySlot)
                {
                    document.ReservedSlots.Add(placement);
                    continue;
                }

                var catalog = ComponentCatalog.Resolve(component.Name);
                var model = new EditorComponent
                {
                    Id = ComponentCatalog.GeneratedComponentId(component.SerialNumber),
                    SchemaId = component.SerialNumber,
                    CatalogTypeId = catalog?.Id,
                    Origin = "generated",
                    RowIndex = rowIndex,
                    SlotStart = placement.SlotStart,
                    Slots = component.Slots,
                    Mounting = catalog?.Mounting == "virtual" ? "virtual" : "din",
                    Amperage = component.Amper,
                    RcdMilliAmps = component is RCD rcd ? rcd.Capacity : null,
                    Phase = PhaseFor(component),
                    LinkedConsumerIds = component is Fuse fuse
                        ? fuse.Electricals
                            .Select(consumer => consumer.Id)
                            .Where(id => id > 0)
                            .Distinct()
                            .OrderBy(id => id)
                            .ToList()
                        : new List<int>()
                };

                componentModels[component.SerialNumber] = model;
                document.Components.Add(model);

                if (catalog == null)
                {
                    document.ImportIssues.Add(new SchemaImportIssue
                    {
                        Code = "unknown_catalog_type",
                        ComponentId = model.Id,
                        Message = $"Unknown legacy device: {component.Name}"
                    });
                }
                else if (catalog.TerminalModel == "legacy-unverified")
                {
                    document.ImportIssues.Add(new SchemaImportIssue
                    {
                        Code = "unverified_terminals",
                        ComponentId = model.Id,
                        Message =
                            "Legacy routing points require terminal review before wire editing."
                    });
                }
            }
        }

        var rcdIds = devices
            .OfType<RCD>()
            .Select(component => component.SerialNumber)
            .ToHashSet();

        foreach (var device in devices.OfType<Fuse>())
        {
            var parentIds = incomingConnections[device.SerialNumber]
                .Select(connection => connection.CabelWay.IndexStart)
                .Where(rcdIds.Contains)
                .Distinct()
                .ToList();

            if (parentIds.Count == 1 &&
                componentModels.TryGetValue(device.SerialNumber, out var child) &&
                componentModels.TryGetValue(parentIds[0], out var parent))
            {
                child.ParentRcdComponentId = parent.Id;
            }
        }

        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var connection in fuseBox.CableConnections)
        {
            var start = connection.CabelWay.IndexStart;
            var finish = connection.CabelWay.IndexFinish;
            var colour = connection.Cable?.Сolour;
            var conductor = ConductorFromColour(colour);
            var baseId =
                $"generated-wire:{start.ToString(CultureInfo.InvariantCulture)}:" +
                $"{finish.ToString(CultureInfo.InvariantCulture)}:" +
                Uri.EscapeDataString((colour ?? "null").ToLowerInvariant());

            var occurrence = occurrences.TryGetValue(baseId, out var count)
                ? count + 1
                : 1;
            occurrences[baseId] = occurrence;

            var id = occurrence == 1
                ? baseId
                : $"{baseId}:duplicate:{occurrence.ToString(CultureInfo.InvariantCulture)}";

            var from = conductor == null
                ? null
                : ResolveEndpoint(
                    componentModels.GetValueOrDefault(start),
                    "output",
                    conductor);

            var to = conductor == null
                ? null
                : ResolveEndpoint(
                    componentModels.GetValueOrDefault(finish),
                    "input",
                    conductor);

            if (conductor != null &&
                from != null &&
                to != null &&
                occurrence == 1)
            {
                document.Connections.Add(new EditorConnection
                {
                    Id = id,
                    From = from,
                    To = to,
                    Conductor = conductor,
                    SectionMm2 = connection.Cable?.Section
                });
            }
            else
            {
                document.UnresolvedConnections.Add(new UnresolvedLegacyConnection
                {
                    Id = id,
                    FromSchemaId = start,
                    ToSchemaId = finish,
                    Colour = colour
                });

                document.ImportIssues.Add(new SchemaImportIssue
                {
                    Code = "unresolved_connection",
                    ConnectionId = id,
                    Message =
                        "Legacy cable needs an unambiguous, reviewed terminal mapping."
                });
            }
        }

        return document;
    }

    private static EditorEndpoint? ResolveEndpoint(
        EditorComponent? component,
        string direction,
        string conductor)
    {
        if (component?.CatalogTypeId == null)
            return null;

        var catalog = ComponentCatalog.ResolveById(component.CatalogTypeId);

        if (catalog?.TerminalModel != "schematic")
            return null;

        if (component.Phase == null &&
            catalog.Terminals.Any(terminal => terminal.PhaseSelectable))
        {
            return null;
        }

        var matches = catalog.Terminals
            .Where(terminal =>
                (terminal.Type == direction || terminal.Type == "universal") &&
                string.Equals(
                    EffectiveConductor(component, terminal),
                    conductor,
                    StringComparison.Ordinal))
            .ToList();

        if (matches.Count != 1)
            return null;

        return new EditorEndpoint
        {
            ComponentId = component.Id,
            TerminalId = matches[0].Id
        };
    }

    internal static string? EffectiveConductor(
        EditorComponent component,
        CatalogTerminal terminal)
    {
        if (!terminal.PhaseSelectable)
            return terminal.Conductor;

        return component.Phase is "L1" or "L2" or "L3"
            ? component.Phase
            : null;
    }

    internal static string? ConductorFromColour(string? colour)
    {
        return colour?.Trim().ToLowerInvariant() switch
        {
            "red" => "L1",
            "orange" => "L2",
            "grey" => "L3",
            "blue" => "N",
            _ => null
        };
    }
}
