using System.Globalization;
using Newtonsoft.Json;

namespace FuseBox.App.SchemaEditor;

/// <summary>
/// Electrical/schema catalog. Built-in entries remain embedded and immutable.
/// Stage 15 adds runtime aliases that inherit a built-in template so admins can
/// add commercial product variants without editing terminals or validation code.
/// </summary>
public static class ComponentCatalog
{
    public const string ResourceName = "FuseBox.SchemaEditor.component-catalog.v1.json";

    private static readonly Lazy<CatalogData> Data = new(Load);
    private static readonly object AliasLock = new();
    private static IReadOnlyDictionary<string, CatalogEntry> _managedAliases =
        new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
    private static IReadOnlySet<string> _unavailableForPlacement =
        new HashSet<string>(StringComparer.Ordinal);

    public static IReadOnlyList<CatalogEntry> BuiltInEntries =>
        Data.Value.Components.AsReadOnly();

    public static IReadOnlyList<CatalogEntry> Entries
    {
        get
        {
            lock (AliasLock)
            {
                if (_managedAliases.Count == 0 &&
                    _unavailableForPlacement.Count == 0)
                {
                    return BuiltInEntries;
                }

                return BuiltInEntries
                    .Where(entry => !_unavailableForPlacement.Contains(entry.Id))
                    .Concat(_managedAliases
                        .Where(pair => !_unavailableForPlacement.Contains(pair.Key))
                        .Select(pair => pair.Value))
                    .ToArray();
            }
        }
    }

    public static string NormalizeName(string? name) =>
        string.Concat((name ?? string.Empty).Where(c => !char.IsWhiteSpace(c)))
            .ToLowerInvariant();

    // Legacy generator names intentionally resolve only built-in entries.
    public static CatalogEntry? Resolve(string? legacyName) =>
        BuiltInEntries.FirstOrDefault(entry =>
            entry.LegacyName == NormalizeName(legacyName));

    public static CatalogEntry? ResolveBuiltInById(string? catalogTypeId)
    {
        if (string.IsNullOrWhiteSpace(catalogTypeId))
            return null;

        var id = catalogTypeId.Trim();
        return BuiltInEntries.FirstOrDefault(entry =>
            string.Equals(entry.Id, id, StringComparison.Ordinal));
    }

    public static CatalogEntry? ResolveById(string? catalogTypeId)
    {
        if (string.IsNullOrWhiteSpace(catalogTypeId))
            return null;

        var id = catalogTypeId.Trim();
        var builtIn = ResolveBuiltInById(id);
        if (builtIn != null)
            return builtIn;

        lock (AliasLock)
        {
            return _managedAliases.GetValueOrDefault(id);
        }
    }

    public static bool IsAvailableForPlacement(string? catalogTypeId)
    {
        if (string.IsNullOrWhiteSpace(catalogTypeId))
            return false;

        var id = catalogTypeId.Trim();
        lock (AliasLock)
        {
            if (_unavailableForPlacement.Contains(id))
                return false;

            var entry = ResolveBuiltInById(id) ??
                        _managedAliases.GetValueOrDefault(id);
            return entry != null &&
                   entry.Mounting == "din" &&
                   entry.Slots > 0 &&
                   entry.Role != "spacer";
        }
    }

    public static void ApplyManagedAliases(IEnumerable<ManagedCatalogAlias> aliases)
    {
        var next = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
        var unavailable = new HashSet<string>(StringComparer.Ordinal);

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias.CatalogTypeId) ||
                string.IsNullOrWhiteSpace(alias.TemplateCatalogTypeId))
                continue;

            var id = alias.CatalogTypeId.Trim();
            if (!alias.Selectable)
                unavailable.Add(id);

            if (alias.IsBuiltIn || ResolveBuiltInById(id) != null)
                continue;

            var template = ResolveBuiltInById(alias.TemplateCatalogTypeId);
            if (template == null)
                continue;

            next[id] = CloneAsAlias(template, id);
        }

        lock (AliasLock)
        {
            _managedAliases = next;
            _unavailableForPlacement = unavailable;
        }
    }

    // Document-scoped identity. Regeneration starts a new generated document;
    // no promise of semantic identity across two generator runs.
    public static string GeneratedComponentId(int schemaId)
    {
        if (schemaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(schemaId));
        return "generated:" + schemaId.ToString(CultureInfo.InvariantCulture);
    }

    private static CatalogEntry CloneAsAlias(CatalogEntry template, string id) =>
        new()
        {
            Id = id,
            // Reusing template legacy name is deliberate: pricing and electrical
            // behavior follow the template while stable product identity is id.
            LegacyName = template.LegacyName,
            Role = template.Role,
            Mounting = template.Mounting,
            Slots = template.Slots,
            TerminalModel = template.TerminalModel,
            Terminals = template.Terminals.Select(CloneTerminal).ToList(),
            LegacyTerminals = template.LegacyTerminals.Select(CloneTerminal).ToList()
        };

    private static CatalogTerminal CloneTerminal(CatalogTerminal terminal) =>
        new()
        {
            Id = terminal.Id,
            Type = terminal.Type,
            Conductor = terminal.Conductor,
            X = terminal.X,
            Y = terminal.Y,
            PhaseSelectable = terminal.PhaseSelectable,
            Color = terminal.Color
        };

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

public sealed record ManagedCatalogAlias(
    string CatalogTypeId,
    string TemplateCatalogTypeId,
    bool IsBuiltIn,
    bool Selectable);

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
