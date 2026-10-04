using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FuseBox.App.SchemaEditor;

namespace FuseBox.Ai;

internal static class SchemaAiCommandFactory
{
    private static readonly HashSet<int> BreakerAmperages = new() { 2, 4, 6, 10, 16, 20, 25, 32 };
    private static readonly HashSet<int> RcdMilliAmps = new() { 10, 30, 100, 300, 500 };
    private static readonly HashSet<string> Phases = new(StringComparer.Ordinal) { "L1", "L2", "L3" };
    private static readonly HashSet<string> LengthSources = new(StringComparer.Ordinal) { "calculated", "estimated", "manual" };

    public static JsonObject Create(string functionName, JsonObject arguments)
    {
        return functionName switch
        {
            "add_schema_component" => ComponentCommand("add_schema_component", arguments, requireId: false),
            "move_schema_component" => new JsonObject
            {
                ["operation"] = "move_schema_component",
                ["componentId"] = RequiredString(arguments, "componentId", 200),
                ["rowIndex"] = RequiredNonNegativeInteger(arguments, "rowIndex"),
                ["slotStart"] = RequiredNonNegativeInteger(arguments, "slotStart"),
            },
            "replace_schema_component" => ComponentCommand("replace_schema_component", arguments, requireId: true),
            "remove_schema_component" => new JsonObject
            {
                ["operation"] = "remove_schema_component",
                ["componentId"] = RequiredString(arguments, "componentId", 200),
            },
            "set_schema_component_phase" => new JsonObject
            {
                ["operation"] = "set_schema_component_phase",
                ["componentId"] = RequiredString(arguments, "componentId", 200),
                ["phase"] = RequiredEnumString(arguments, "phase", Phases),
            },
            "connect_terminals" => Connect(arguments),
            "disconnect_terminals" => new JsonObject
            {
                ["operation"] = "disconnect_terminals",
                ["connectionId"] = RequiredString(arguments, "connectionId", 300),
            },
            "update_schema_connection" => UpdateConnection(arguments),
            _ => throw new InvalidOperationException($"Unknown schema editor tool: {functionName}."),
        };
    }

    private static JsonObject ComponentCommand(string operation, JsonObject arguments, bool requireId)
    {
        var catalogTypeId = RequiredString(arguments, "catalogTypeId", 120);
        var catalog = ComponentCatalog.ResolveById(catalogTypeId);

        if (catalog == null || catalog.Mounting != "din" || catalog.Slots <= 0 || catalog.Role == "spacer")
            throw new InvalidOperationException("catalogTypeId is not a placeable DIN component.");

        var result = new JsonObject
        {
            ["operation"] = operation,
            ["catalogTypeId"] = catalogTypeId,
        };

        if (requireId)
            result["componentId"] = RequiredString(arguments, "componentId", 200);
        else
        {
            result["componentId"] = "manual:" + Guid.NewGuid().ToString("N");
            result["rowIndex"] = RequiredNonNegativeInteger(arguments, "rowIndex");
            result["slotStart"] = RequiredNonNegativeInteger(arguments, "slotStart");
        }

        CopyOptionalInteger(arguments, result, "amperage", BreakerAmperages);
        CopyOptionalInteger(arguments, result, "rcdMilliAmps", RcdMilliAmps);
        CopyOptionalEnumString(arguments, result, "phase", Phases);
        return result;
    }

    private static JsonObject Connect(JsonObject arguments)
    {
        var result = new JsonObject
        {
            ["operation"] = "connect_terminals",
            ["connectionId"] = "manual-wire:" + Guid.NewGuid().ToString("N"),
            ["fromComponentId"] = RequiredString(arguments, "fromComponentId", 200),
            ["fromTerminalId"] = RequiredString(arguments, "fromTerminalId", 100),
            ["toComponentId"] = RequiredString(arguments, "toComponentId", 200),
            ["toTerminalId"] = RequiredString(arguments, "toTerminalId", 100),
        };

        CopyOptionalPositiveNumber(arguments, result, "sectionMm2", allowZero: false);
        CopyOptionalPositiveNumber(arguments, result, "lengthMeters", allowZero: true);
        CopyOptionalEnumString(arguments, result, "lengthSource", LengthSources);
        return result;
    }

    private static JsonObject UpdateConnection(JsonObject arguments)
    {
        var result = new JsonObject
        {
            ["operation"] = "update_schema_connection",
            ["connectionId"] = RequiredString(arguments, "connectionId", 300),
        };

        CopyOptionalPositiveNumber(arguments, result, "sectionMm2", allowZero: false);
        CopyOptionalPositiveNumber(arguments, result, "lengthMeters", allowZero: true);
        CopyOptionalEnumString(arguments, result, "lengthSource", LengthSources);
        return result;
    }

    private static string RequiredString(JsonObject value, string key, int maxLength)
    {
        if (value[key] is not JsonValue node || !node.TryGetValue<string>(out var text))
            throw new InvalidOperationException($"{key} must be a string.");

        text = text.Trim();
        if (text.Length == 0 || text.Length > maxLength)
            throw new InvalidOperationException($"{key} must contain 1 to {maxLength} characters.");
        return text;
    }

    private static int RequiredNonNegativeInteger(JsonObject value, string key)
    {
        if (value[key] is not JsonValue node || !node.TryGetValue<int>(out var result) || result < 0)
            throw new InvalidOperationException($"{key} must be a non-negative integer.");
        return result;
    }

    private static string RequiredEnumString(JsonObject value, string key, HashSet<string> allowed)
    {
        var text = RequiredString(value, key, 40);
        if (!allowed.Contains(text)) throw new InvalidOperationException($"{key} has an unsupported value.");
        return text;
    }

    private static void CopyOptionalInteger(JsonObject source, JsonObject target, string key, HashSet<int> allowed)
    {
        if (source[key] is null) return;
        if (source[key] is not JsonValue node || !node.TryGetValue<int>(out var value) || !allowed.Contains(value))
            throw new InvalidOperationException($"{key} has an unsupported value.");
        target[key] = value;
    }

    private static void CopyOptionalEnumString(JsonObject source, JsonObject target, string key, HashSet<string> allowed)
    {
        if (source[key] is null) return;
        target[key] = RequiredEnumString(source, key, allowed);
    }

    private static void CopyOptionalPositiveNumber(JsonObject source, JsonObject target, string key, bool allowZero)
    {
        if (source[key] is null) return;
        if (source[key] is not JsonValue node || !node.TryGetValue<double>(out var value) || !double.IsFinite(value) || (allowZero ? value < 0 : value <= 0))
            throw new InvalidOperationException($"{key} has an invalid value.");
        target[key] = value;
    }
}
