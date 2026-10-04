namespace FuseBox.App.SchemaEditor;

public sealed class SchemaValidationContext
{
    public int ShieldWidth { get; init; }
    public int RailCount { get; init; }
    public IReadOnlyCollection<int> RequiredConsumerIds { get; init; } =
        Array.Empty<int>();
}

public interface ISchemaEditorValidationRule
{
    void Validate(
        SchemaDocument document,
        SchemaValidationContext context,
        IList<SchemaValidationError> errors);
}

public sealed class SchemaEditorValidator
{
    private readonly IReadOnlyList<ISchemaEditorValidationRule> _rules;

    public SchemaEditorValidator(
        IEnumerable<ISchemaEditorValidationRule> rules)
    {
        _rules = rules.ToList();
    }

    public SchemaValidationResponse Validate(
        SchemaDocument document,
        SchemaValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        var errors = new List<SchemaValidationError>();

        if (context.ShieldWidth <= 0 || context.RailCount <= 0)
        {
            errors.Add(new SchemaValidationError
            {
                Code = "panel_geometry_invalid",
                Message = "The generated panel has invalid DIN rail geometry."
            });

            return new SchemaValidationResponse { Errors = errors };
        }

        foreach (var rule in _rules)
            rule.Validate(document, context, errors);

        return new SchemaValidationResponse { Errors = errors };
    }
}

public sealed class ComponentPlacementValidationRule : ISchemaEditorValidationRule
{
    private const int MaxComponents = 1024;
    private const int MaxIdLength = 128;

    public void Validate(
        SchemaDocument document,
        SchemaValidationContext context,
        IList<SchemaValidationError> errors)
    {
        if (document.Components.Count == 0)
        {
            errors.Add(new SchemaValidationError
            {
                Code = "component_list_empty",
                Message = "A saved schema must contain at least one component."
            });
        }
        else if (document.Components.Count > MaxComponents)
        {
            errors.Add(new SchemaValidationError
            {
                Code = "component_limit_exceeded",
                Message = $"A schema can contain at most {MaxComponents} components."
            });
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var schemaIds = new HashSet<int>();
        var occupancy = new Dictionary<(int Row, int Slot), string>();

        foreach (var component in document.Components)
        {
            if (string.IsNullOrWhiteSpace(component.Id) ||
                component.Id.Length > MaxIdLength)
            {
                errors.Add(ComponentError(
                    "component_id_invalid",
                    component,
                    "Component ID is missing or too long."));
                continue;
            }

            if (!ids.Add(component.Id))
            {
                errors.Add(ComponentError(
                    "component_id_duplicate",
                    component,
                    "Component IDs must be unique."));
            }

            if (component.SchemaId.HasValue)
            {
                if (component.SchemaId.Value <= 0)
                {
                    errors.Add(ComponentError(
                        "schema_id_invalid",
                        component,
                        "SchemaId must be positive when present."));
                }
                else if (!schemaIds.Add(component.SchemaId.Value))
                {
                    errors.Add(ComponentError(
                        "schema_id_duplicate",
                        component,
                        "SchemaId values must be unique when present."));
                }
            }

            if (component.Origin is not ("generated" or "manual"))
            {
                errors.Add(ComponentError(
                    "component_origin_invalid",
                    component,
                    "Component origin must be generated or manual."));
            }

            var catalog = ComponentCatalog.ResolveById(component.CatalogTypeId);

            if (catalog == null)
            {
                errors.Add(ComponentError(
                    "catalog_type_invalid",
                    component,
                    "Component catalogTypeId is not present in the component catalog."));
                continue;
            }

            if (catalog.Mounting == "spacer")
            {
                errors.Add(ComponentError(
                    "catalog_type_not_device",
                    component,
                    "Layout spacer catalog entries cannot be saved as editor components."));
                continue;
            }

            var expectedMounting = catalog.Mounting == "virtual"
                ? "virtual"
                : "din";

            if (!string.Equals(
                    component.Mounting,
                    expectedMounting,
                    StringComparison.Ordinal))
            {
                errors.Add(ComponentError(
                    "component_mounting_invalid",
                    component,
                    $"Component mounting must be {expectedMounting}."));
            }

            if (component.Slots != catalog.Slots)
            {
                errors.Add(ComponentError(
                    "component_slots_invalid",
                    component,
                    $"Component requires {catalog.Slots} DIN slot(s)."));
            }

            if (component.RowIndex < 0 ||
                component.RowIndex >= context.RailCount)
            {
                errors.Add(ComponentError(
                    "component_row_invalid",
                    component,
                    "Component rowIndex is outside the generated panel rails."));
                continue;
            }

            if (component.SlotStart < 0 ||
                component.SlotStart > context.ShieldWidth)
            {
                errors.Add(ComponentError(
                    "component_slot_invalid",
                    component,
                    "Component slotStart is outside the DIN rail."));
                continue;
            }

            if (expectedMounting == "din")
            {
                if (component.Slots <= 0 ||
                    component.SlotStart + component.Slots > context.ShieldWidth)
                {
                    errors.Add(ComponentError(
                        "component_out_of_bounds",
                        component,
                        "Component does not fit on the selected DIN rail."));
                    continue;
                }

                for (var slot = component.SlotStart;
                     slot < component.SlotStart + component.Slots;
                     slot++)
                {
                    var key = (component.RowIndex, slot);

                    if (occupancy.TryGetValue(key, out var otherId))
                    {
                        errors.Add(ComponentError(
                            "component_overlap",
                            component,
                            $"Component overlaps {otherId} on row {component.RowIndex + 1}."));
                        break;
                    }

                    occupancy[key] = component.Id;
                }
            }
            else if (component.Slots != 0)
            {
                errors.Add(ComponentError(
                    "virtual_component_slots_invalid",
                    component,
                    "Virtual components must occupy zero DIN slots."));
            }

            if (component.Phase != null &&
                component.Phase is not ("L1" or "L2" or "L3"))
            {
                errors.Add(ComponentError(
                    "component_phase_invalid",
                    component,
                    "Component phase must be L1, L2 or L3 when specified."));
            }

            if (component.Amperage.HasValue &&
                (!double.IsFinite(component.Amperage.Value) ||
                 component.Amperage.Value < 0 ||
                 component.Amperage.Value > 1000))
            {
                errors.Add(ComponentError(
                    "component_amperage_invalid",
                    component,
                    "Component amperage is outside the supported range."));
            }

            if (component.LinkedConsumerIds.Any(id => id <= 0) ||
                component.LinkedConsumerIds.Distinct().Count() !=
                component.LinkedConsumerIds.Count)
            {
                errors.Add(ComponentError(
                    "linked_consumers_invalid",
                    component,
                    "linkedConsumerIds must contain unique positive IDs."));
            }
        }

        foreach (var placement in document.ReservedSlots)
        {
            if (placement.RowIndex < 0 ||
                placement.RowIndex >= context.RailCount ||
                placement.SlotStart < 0 ||
                placement.Slots < 0 ||
                placement.SlotStart + placement.Slots > context.ShieldWidth)
            {
                errors.Add(new SchemaValidationError
                {
                    Code = "reserved_slots_invalid",
                    RowIndex = placement.RowIndex,
                    Message = "Reserved layout slots are outside the panel geometry."
                });
            }
        }
    }

    private static SchemaValidationError ComponentError(
        string code,
        EditorComponent component,
        string message)
    {
        return new SchemaValidationError
        {
            Code = code,
            ComponentId = string.IsNullOrWhiteSpace(component.Id)
                ? null
                : component.Id,
            RowIndex = component.RowIndex,
            Message = message
        };
    }
}

public sealed class ConnectionValidationRule : ISchemaEditorValidationRule
{
    private const int MaxConnections = 8192;
    private const int MaxIdLength = 160;

    public void Validate(
        SchemaDocument document,
        SchemaValidationContext context,
        IList<SchemaValidationError> errors)
    {
        if (document.Connections.Count > MaxConnections)
        {
            errors.Add(new SchemaValidationError
            {
                Code = "connection_limit_exceeded",
                Message = $"A schema can contain at most {MaxConnections} connections."
            });
        }

        var components = document.Components
            .Where(component => !string.IsNullOrWhiteSpace(component.Id))
            .GroupBy(component => component.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        var occupiedTargetTerminals = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (var connection in document.Connections)
        {
            if (string.IsNullOrWhiteSpace(connection.Id) ||
                connection.Id.Length > MaxIdLength)
            {
                errors.Add(ConnectionError(
                    "connection_id_invalid",
                    connection,
                    "Connection ID is missing or too long."));
            }
            else if (!ids.Add(connection.Id))
            {
                errors.Add(ConnectionError(
                    "connection_id_duplicate",
                    connection,
                    "Connection IDs must be unique."));
            }

            if (!IsConductor(connection.Conductor))
            {
                errors.Add(ConnectionError(
                    "connection_conductor_invalid",
                    connection,
                    "Connection conductor must be L1, L2, L3, N or PE."));
                continue;
            }

            var signature =
                $"{connection.From.ComponentId}\u001f{connection.From.TerminalId}\u001f" +
                $"{connection.To.ComponentId}\u001f{connection.To.TerminalId}\u001f" +
                connection.Conductor;

            if (!signatures.Add(signature))
            {
                errors.Add(ConnectionError(
                    "connection_duplicate",
                    connection,
                    "Duplicate terminal connection is not allowed."));
            }

            if (connection.From.ComponentId == connection.To.ComponentId)
            {
                errors.Add(ConnectionError(
                    "connection_self_connection",
                    connection,
                    "A component cannot be electrically connected to itself."));
            }

            var targetKey =
                $"{connection.To.ComponentId}\u001f{connection.To.TerminalId}";

            if (occupiedTargetTerminals.TryGetValue(
                    targetKey,
                    out var occupyingConnectionId))
            {
                errors.Add(ConnectionError(
                    "connection_terminal_occupied",
                    connection,
                    "The target terminal is already occupied by " +
                    $"connection {occupyingConnectionId}."));
            }
            else
            {
                occupiedTargetTerminals[targetKey] =
                    string.IsNullOrWhiteSpace(connection.Id)
                        ? "another connection"
                        : connection.Id;
            }

            var from = ResolveTerminal(
                connection,
                connection.From,
                components,
                "from",
                errors);

            var to = ResolveTerminal(
                connection,
                connection.To,
                components,
                "to",
                errors);

            if (from == null || to == null)
                continue;

            if (from.Value.Terminal.Type is not ("output" or "universal"))
            {
                errors.Add(ConnectionError(
                    "connection_from_direction_invalid",
                    connection,
                    "The source terminal must be output or universal."));
            }

            if (to.Value.Terminal.Type is not ("input" or "universal"))
            {
                errors.Add(ConnectionError(
                    "connection_to_direction_invalid",
                    connection,
                    "The target terminal must be input or universal."));
            }

            var fromConductor = GeneratedSchemaDocumentFactory.EffectiveConductor(
                from.Value.Component,
                from.Value.Terminal);
            var toConductor = GeneratedSchemaDocumentFactory.EffectiveConductor(
                to.Value.Component,
                to.Value.Terminal);

            if (fromConductor == null || toConductor == null)
            {
                errors.Add(ConnectionError(
                    "terminal_phase_ambiguous",
                    connection,
                    "A phase-selectable terminal requires an explicit component phase."));
                continue;
            }

            if (!string.Equals(
                    fromConductor,
                    connection.Conductor,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    toConductor,
                    connection.Conductor,
                    StringComparison.Ordinal))
            {
                errors.Add(ConnectionError(
                    "conductor_incompatible",
                    connection,
                    "Connection conductor is incompatible with one or both terminals."));
            }

            if (connection.SectionMm2.HasValue && connection.SectionMm2.Value <= 0)
            {
                errors.Add(ConnectionError(
                    "connection_section_invalid",
                    connection,
                    "Wire section must be greater than zero when specified."));
            }

            if (connection.LengthMeters.HasValue && connection.LengthMeters.Value < 0)
            {
                errors.Add(ConnectionError(
                    "connection_length_invalid",
                    connection,
                    "Wire length cannot be negative."));
            }

            if (connection.LengthSource != null &&
                connection.LengthSource is not ("calculated" or "estimated" or "manual"))
            {
                errors.Add(ConnectionError(
                    "connection_length_source_invalid",
                    connection,
                    "Wire lengthSource is invalid."));
            }
        }
    }

    private static (EditorComponent Component, CatalogTerminal Terminal)? ResolveTerminal(
        EditorConnection connection,
        EditorEndpoint endpoint,
        IReadOnlyDictionary<string, EditorComponent> components,
        string endpointName,
        IList<SchemaValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(endpoint.ComponentId) ||
            !components.TryGetValue(endpoint.ComponentId, out var component))
        {
            errors.Add(ConnectionError(
                $"connection_{endpointName}_component_missing",
                connection,
                $"The {endpointName} component does not exist."));
            return null;
        }

        var catalog = ComponentCatalog.ResolveById(component.CatalogTypeId);

        if (catalog == null)
        {
            errors.Add(ConnectionError(
                $"connection_{endpointName}_catalog_missing",
                connection,
                $"The {endpointName} component has no valid catalog type."));
            return null;
        }

        var terminal = catalog.Terminals.FirstOrDefault(candidate =>
            string.Equals(
                candidate.Id,
                endpoint.TerminalId,
                StringComparison.Ordinal));

        if (terminal == null)
        {
            errors.Add(ConnectionError(
                $"connection_{endpointName}_terminal_missing",
                connection,
                $"The {endpointName} terminal does not exist on the selected component."));
            return null;
        }

        return (component, terminal);
    }

    private static bool IsConductor(string conductor) =>
        conductor is "L1" or "L2" or "L3" or "N" or "PE";

    private static SchemaValidationError ConnectionError(
        string code,
        EditorConnection connection,
        string message)
    {
        return new SchemaValidationError
        {
            Code = code,
            ConnectionId = string.IsNullOrWhiteSpace(connection.Id)
                ? null
                : connection.Id,
            Message = message
        };
    }
}


public sealed class ConsumerLinkValidationRule : ISchemaEditorValidationRule
{
    public void Validate(
        SchemaDocument document,
        SchemaValidationContext context,
        IList<SchemaValidationError> errors)
    {
        var required = context.RequiredConsumerIds
            .Where(id => id > 0)
            .ToHashSet();

        if (required.Count == 0)
            return;

        var owners = new Dictionary<int, string>();

        foreach (var component in document.Components)
        {
            foreach (var consumerId in component.LinkedConsumerIds)
            {
                if (!required.Contains(consumerId))
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "linked_consumer_unknown",
                        ComponentId = component.Id,
                        RowIndex = component.RowIndex,
                        Message =
                            $"Consumer {consumerId} does not belong to this project."
                    });
                    continue;
                }

                if (owners.TryGetValue(consumerId, out var ownerId))
                {
                    errors.Add(new SchemaValidationError
                    {
                        Code = "linked_consumer_duplicate",
                        ComponentId = component.Id,
                        RowIndex = component.RowIndex,
                        Message =
                            $"Consumer {consumerId} is linked to both {ownerId} and {component.Id}."
                    });
                    continue;
                }

                owners[consumerId] = component.Id;
            }
        }

        foreach (var consumerId in required.OrderBy(id => id))
        {
            if (owners.ContainsKey(consumerId))
                continue;

            errors.Add(new SchemaValidationError
            {
                Code = "consumer_protection_missing",
                Message =
                    $"Consumer {consumerId} is not linked to any schema component."
            });
        }
    }
}

public sealed class BasicElectricalValidationRule : ISchemaEditorValidationRule
{
    private static readonly HashSet<int> SupportedRcdSensitivities =
        new() { 10, 30, 100, 300, 500 };

    public void Validate(
        SchemaDocument document,
        SchemaValidationContext context,
        IList<SchemaValidationError> errors)
    {
        var components = document.Components
            .Where(component => !string.IsNullOrWhiteSpace(component.Id))
            .GroupBy(component => component.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var incomingPhaseComponents = document.Connections
            .Where(connection =>
                connection.Conductor is "L1" or "L2" or "L3")
            .Select(connection => connection.To.ComponentId)
            .ToHashSet(StringComparer.Ordinal);

        var graphIsFullyResolved = document.UnresolvedConnections.Count == 0;

        foreach (var component in document.Components)
        {
            var catalog = ComponentCatalog.ResolveById(component.CatalogTypeId);

            if (catalog == null)
                continue;

            if (component.ParentRcdComponentId != null)
            {
                if (!components.TryGetValue(
                        component.ParentRcdComponentId,
                        out var parent) ||
                    ComponentCatalog.ResolveById(parent.CatalogTypeId)?.Role != "rcd")
                {
                    errors.Add(ComponentError(
                        "parent_rcd_invalid",
                        component,
                        "parentRcdComponentId must reference an RCD component."));
                }
            }

            if (catalog.Role == "mcb")
            {
                if (!component.Amperage.HasValue ||
                    !double.IsFinite(component.Amperage.Value) ||
                    component.Amperage.Value <= 0 ||
                    component.Amperage.Value > 125)
                {
                    errors.Add(ComponentError(
                        "mcb_nominal_invalid",
                        component,
                        "MCB nominal current must be greater than 0 A and at most 125 A."));
                }

                if (graphIsFullyResolved &&
                    !incomingPhaseComponents.Contains(component.Id))
                {
                    errors.Add(ComponentError(
                        "upstream_protection_missing",
                        component,
                        "MCB has no resolved upstream phase connection."));
                }
            }

            if (catalog.Role == "rcd" && component.RcdMilliAmps.HasValue &&
                !SupportedRcdSensitivities.Contains(component.RcdMilliAmps.Value))
            {
                errors.Add(ComponentError(
                    "rcd_sensitivity_invalid",
                    component,
                    "RCD sensitivity must be 10, 30, 100, 300 or 500 mA."));
            }
        }
    }

    private static SchemaValidationError ComponentError(
        string code,
        EditorComponent component,
        string message)
    {
        return new SchemaValidationError
        {
            Code = code,
            ComponentId = component.Id,
            RowIndex = component.RowIndex,
            Message = message
        };
    }
}
