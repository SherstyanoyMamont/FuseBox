using System.Globalization;
using Newtonsoft.Json;

namespace FuseBox.App.SchemaEditor;

/// <summary>
/// Existing generator device types, shared with frontend component-catalog.v1.json.
/// No manufacturer pinout or product substitution is inferred from legacy routing.
/// </summary>
public static class ComponentCatalog
{
    public const string ResourceName = "FuseBox.SchemaEditor.component-catalog.v1.json";

    private static readonly Lazy<CatalogData> Data = new(Load);
    public static IReadOnlyList<CatalogEntry> Entries => Data.Value.Components.AsReadOnly();

    public static string NormalizeName(string? name) =>
        string.Concat((name ?? string.Empty).Where(c => !char.IsWhiteSpace(c)))
            .ToLowerInvariant();

    public static CatalogEntry? Resolve(string? legacyName) =>
        Entries.FirstOrDefault(entry => entry.LegacyName == NormalizeName(legacyName));

    public static CatalogEntry? ResolveById(string? catalogTypeId)
    {
        if (string.IsNullOrWhiteSpace(catalogTypeId))
            return null;

        return Entries.FirstOrDefault(entry =>
            string.Equals(
                entry.Id,
                catalogTypeId.Trim(),
                StringComparison.Ordinal));
    }

    // Document-scoped identity. Regeneration starts a new generated document;
    // no promise of semantic identity across two generator runs.
    public static string GeneratedComponentId(int schemaId)
    {
        if (schemaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(schemaId));
        return "generated:" + schemaId.ToString(CultureInfo.InvariantCulture);
    }

    private static CatalogData Load()
    {
        var assembly = typeof(ComponentCatalog).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new SchemaCatalogException(
                $"Schema component catalog resource '{ResourceName}' is missing from " +
                $"'{assembly.FullName}'. Apply FuseBox.csproj with WithCulture=false, " +
                "clean, rebuild and restart the backend. Manifest resources: " +
                string.Join(", ", assembly.GetManifestResourceNames()));
        using var reader = new StreamReader(stream);
        var data = JsonConvert.DeserializeObject<CatalogData>(reader.ReadToEnd())
            ?? throw new SchemaCatalogException("Schema component catalog is invalid.");
        if (data.Version != 1 ||
            data.Components.Select(entry => entry.Id).Distinct().Count() != data.Components.Count ||
            data.Components.Select(entry => entry.LegacyName).Distinct().Count() != data.Components.Count)
            throw new SchemaCatalogException("Unsupported or duplicate schema catalog entries.");
        foreach (var entry in data.Components)
        {
            if (entry.Terminals.Select(t => t.Id).Distinct().Count() != entry.Terminals.Count ||
                entry.LegacyTerminals.Select(t => t.Id).Distinct().Count() != entry.LegacyTerminals.Count)
                throw new SchemaCatalogException("Duplicate terminal ID in " + entry.Id);
        }
        return data;
    }

    private sealed class CatalogData
    {
        public int Version { get; set; }
        public List<CatalogEntry> Components { get; set; } = new();
    }
}

public sealed class CatalogEntry
{
    public string Id { get; set; } = string.Empty;
    public string LegacyName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Mounting { get; set; } = "din";
    public int Slots { get; set; }
    public string TerminalModel { get; set; } = "legacy-unverified";
    public List<CatalogTerminal> Terminals { get; set; } = new();
    public List<CatalogTerminal> LegacyTerminals { get; set; } = new();
}

public sealed class CatalogTerminal
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Conductor { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public bool PhaseSelectable { get; set; }
    public string? Color { get; set; }
}


// A deployment/catalog failure is not evidence of an inconsistent stored project.
public sealed class SchemaCatalogException : Exception
{
    public SchemaCatalogException(string message) : base(message) { }
}


