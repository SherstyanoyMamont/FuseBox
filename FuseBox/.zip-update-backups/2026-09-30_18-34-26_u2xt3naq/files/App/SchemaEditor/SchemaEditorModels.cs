using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FuseBox.App.SchemaEditor;

// Stage 1 read-model contracts only: these classes are not EF entities.
// Persistent state, optimistic concurrency and save validation belong to Stage 2.
public static class SchemaModes
{
    public const string Generated = "generated";
    public const string Customized = "customized";
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed class SchemaDocument
{
    public string Mode { get; set; } = SchemaModes.Generated;
    public int Revision { get; set; } = 1;
    public List<EditorComponent> Components { get; set; } = new();
    public List<EditorConnection> Connections { get; set; } = new();
    public List<DinPlacement> ReservedSlots { get; set; } = new();
    public List<UnresolvedLegacyConnection> UnresolvedConnections { get; set; } = new();
    public List<SchemaImportIssue> ImportIssues { get; set; } = new();
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed class EditorComponent
{
    public string Id { get; set; } = string.Empty;
    public int? SchemaId { get; set; }
    public string? CatalogTypeId { get; set; }
    public string Origin { get; set; } = "generated";
    public int RowIndex { get; set; }
    public int SlotStart { get; set; }
    public int Slots { get; set; }
    public string Mounting { get; set; } = "din";
    public double? Amperage { get; set; }
    public int? RcdMilliAmps { get; set; }
    public string? Phase { get; set; }
    public string? ParentRcdComponentId { get; set; }
    public List<int> LinkedConsumerIds { get; set; } = new();
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed class EditorEndpoint
{
    public string ComponentId { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed class EditorConnection
{
    public string Id { get; set; } = string.Empty;
    public EditorEndpoint From { get; set; } = new();
    public EditorEndpoint To { get; set; } = new();
    public string Conductor { get; set; } = string.Empty;
    public decimal? SectionMm2 { get; set; }
    public decimal? LengthMeters { get; set; }
    public string? LengthSource { get; set; }
}


[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed class DinPlacement
{
    public int RowIndex { get; set; }
    public int SlotStart { get; set; }
    public int Slots { get; set; }
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed class UnresolvedLegacyConnection
{
    public string Id { get; set; } = string.Empty;
    public int FromSchemaId { get; set; }
    public int ToSchemaId { get; set; }
    public string? Colour { get; set; }
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public sealed class SchemaImportIssue
{
    public string Code { get; set; } = string.Empty;
    public string? ComponentId { get; set; }
    public string? ConnectionId { get; set; }
    public string Message { get; set; } = string.Empty;
}


