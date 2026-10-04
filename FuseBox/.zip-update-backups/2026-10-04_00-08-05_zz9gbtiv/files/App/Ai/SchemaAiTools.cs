using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using FuseBox.App.SchemaEditor;

namespace FuseBox.Ai;

internal static class SchemaAiTools
{
    private static readonly int[] BreakerAmperages = { 2, 4, 6, 10, 16, 20, 25, 32 };
    private static readonly int[] RcdMilliAmps = { 10, 30, 100, 300, 500 };

    public static JsonArray CreateAll()
    {
        return new JsonArray
        {
            Function(
                "add_schema_component",
                "Add one component to a free logical DIN row/slot in the local schema draft. Use a catalogTypeId from the supplied catalog. New phase-selectable devices default to L1 when phase is omitted.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["catalogTypeId"] = CatalogIdSchema(),
                        ["rowIndex"] = NonNegativeInteger(),
                        ["slotStart"] = NonNegativeInteger(),
                        ["amperage"] = OptionalIntegerEnum(BreakerAmperages),
                        ["rcdMilliAmps"] = OptionalIntegerEnum(RcdMilliAmps),
                        ["phase"] = OptionalStringEnum("L1", "L2", "L3"),
                    },
                    new[] { "catalogTypeId", "rowIndex", "slotStart" }
                )
            ),
            Function(
                "move_schema_component",
                "Move an existing component to a free logical DIN row/slot. Use the exact componentId from the current snapshot.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["componentId"] = StringSchema(200),
                        ["rowIndex"] = NonNegativeInteger(),
                        ["slotStart"] = NonNegativeInteger(),
                    },
                    new[] { "componentId", "rowIndex", "slotStart" }
                )
            ),
            Function(
                "replace_schema_component",
                "Replace an existing component with a compatible catalog variant while preserving its stable identity, origin, position and consumer linkage. Use the exact componentId from the snapshot.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["componentId"] = StringSchema(200),
                        ["catalogTypeId"] = CatalogIdSchema(),
                        ["amperage"] = OptionalIntegerEnum(BreakerAmperages),
                        ["rcdMilliAmps"] = OptionalIntegerEnum(RcdMilliAmps),
                        ["phase"] = OptionalStringEnum("L1", "L2", "L3"),
                    },
                    new[] { "componentId", "catalogTypeId" }
                )
            ),
            Function(
                "remove_schema_component",
                "Remove one component from the local draft. Do not silently remove its other electrical dependencies; frontend/backend validation will surface any invalid intermediate state.",
                ObjectSchema(
                    new JsonObject { ["componentId"] = StringSchema(200) },
                    new[] { "componentId" }
                )
            ),
            Function(
                "set_schema_component_phase",
                "Set L1, L2 or L3 on an existing phase-selectable component.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["componentId"] = StringSchema(200),
                        ["phase"] = StringEnum("L1", "L2", "L3"),
                    },
                    new[] { "componentId", "phase" }
                )
            ),
            Function(
                "connect_terminals",
                "Create a terminal-to-terminal connection in the local draft. Use exact stable componentId and terminalId values. Source must be output/universal, target input/universal, and conductor/phase compatibility must hold.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["fromComponentId"] = StringSchema(200),
                        ["fromTerminalId"] = StringSchema(100),
                        ["toComponentId"] = StringSchema(200),
                        ["toTerminalId"] = StringSchema(100),
                        ["sectionMm2"] = OptionalPositiveNumber(),
                        ["lengthMeters"] = OptionalNonNegativeNumber(),
                        ["lengthSource"] = OptionalStringEnum("calculated", "estimated", "manual"),
                    },
                    new[] { "fromComponentId", "fromTerminalId", "toComponentId", "toTerminalId" }
                )
            ),
            Function(
                "disconnect_terminals",
                "Remove an existing connection from the local draft by its exact connectionId.",
                ObjectSchema(
                    new JsonObject { ["connectionId"] = StringSchema(300) },
                    new[] { "connectionId" }
                )
            ),
            Function(
                "update_schema_connection",
                "Update optional wire section/length metadata of an existing resolved connection without changing its endpoints.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["connectionId"] = StringSchema(300),
                        ["sectionMm2"] = OptionalPositiveNumber(),
                        ["lengthMeters"] = OptionalNonNegativeNumber(),
                        ["lengthSource"] = OptionalStringEnum("calculated", "estimated", "manual"),
                    },
                    new[] { "connectionId" }
                )
            ),
        };
    }

    private static JsonObject CatalogIdSchema()
    {
        var ids = ComponentCatalog.Entries
            .Where(entry => entry.Mounting == "din" && entry.Slots > 0 && entry.Role != "spacer")
            .Select(entry => (JsonNode?)JsonValue.Create(entry.Id))
            .ToArray();

        return new JsonObject
        {
            ["type"] = "string",
            ["enum"] = new JsonArray(ids),
        };
    }

    private static JsonObject Function(string name, string description, JsonObject parameters) =>
        new()
        {
            ["type"] = "function",
            ["name"] = name,
            ["description"] = description,
            ["parameters"] = parameters,
            ["strict"] = false,
        };

    private static JsonObject ObjectSchema(JsonObject properties, IEnumerable<string> required) =>
        new()
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray(required.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()),
            ["additionalProperties"] = false,
        };

    private static JsonObject StringSchema(int maxLength) => new()
    {
        ["type"] = "string",
        ["minLength"] = 1,
        ["maxLength"] = maxLength,
    };

    private static JsonObject NonNegativeInteger() => new()
    {
        ["type"] = "integer",
        ["minimum"] = 0,
    };

    private static JsonObject StringEnum(params string[] values) => new()
    {
        ["type"] = "string",
        ["enum"] = new JsonArray(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()),
    };

    private static JsonObject OptionalStringEnum(params string[] values) => new()
    {
        ["type"] = StringArray("string", "null"),
        ["enum"] = new JsonArray(
            values
                .Select(value => (JsonNode?)JsonValue.Create(value))
                .Concat(new JsonNode?[] { null })
                .ToArray()),
    };

    private static JsonObject OptionalIntegerEnum(params int[] values) => new()
    {
        ["type"] = StringArray("integer", "null"),
        ["enum"] = new JsonArray(
            values
                .Select(value => (JsonNode?)JsonValue.Create(value))
                .Concat(new JsonNode?[] { null })
                .ToArray()),
    };

    private static JsonObject OptionalPositiveNumber() => new()
    {
        ["type"] = StringArray("number", "null"),
        ["exclusiveMinimum"] = 0,
    };

    private static JsonObject OptionalNonNegativeNumber() => new()
    {
        ["type"] = StringArray("number", "null"),
        ["minimum"] = 0,
    };

    private static JsonArray StringArray(params string[] values) =>
        new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
}
