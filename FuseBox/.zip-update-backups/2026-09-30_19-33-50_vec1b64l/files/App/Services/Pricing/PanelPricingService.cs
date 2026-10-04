using System.Globalization;
using global::FuseBox;
using FuseBox.App.SchemaEditor;
using FuseBox.FuseBox;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Services.Pricing;

public sealed class PanelPricingService
{
    private readonly TmeApiClient _tme;
    private readonly PricingOptions _options;

    public PanelPricingService(
        TmeApiClient tme,
        IOptions<PricingOptions> options)
    {
        _tme = tme;
        _options = options.Value;
    }

    public async Task<ProjectPricingResponse> CalculateAsync(
        Project project,
        InitialSettings settings,
        SchemaDocument activeSchema,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>
        {
            "Schneider reference prices are static public list-price snapshots in this first stage. TME Germany is the live market source; a live Schneider provider can replace the snapshots later."
        };

        var candidates = BuildCatalogCandidates(
            activeSchema,
            settings,
            warnings);

        var mpns = candidates
            .Where(candidate => candidate.Catalog != null)
            .Select(candidate => candidate.Catalog!.Mpn)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var quotes = await _tme.GetQuotesAsync(
            mpns,
            forceRefresh,
            cancellationToken);

        var lines = new List<PricingLine>();

        foreach (var candidate in candidates)
        {
            if (candidate.Catalog == null)
            {
                lines.Add(new PricingLine
                {
                    Category = candidate.Category,
                    Name = candidate.Name,
                    Quantity = candidate.Quantity,
                    Unit = "pcs",
                    PriceSource = "unmapped",
                    Note = candidate.Note
                });
                continue;
            }

            var catalog = candidate.Catalog;
            quotes.TryGetValue(
                TmePriceCacheStore.NormalizeKey(catalog.Mpn),
                out var quote);

            var marketUnit = quote?.GetUnitNetForQuantity(
                candidate.Quantity);

            lines.Add(CreateCatalogLine(
                candidate.Category,
                catalog,
                candidate.Quantity,
                marketUnit,
                quote,
                candidate.Note));
        }

        AddWireLines(activeSchema, lines, warnings);

        foreach (var quote in quotes.Values.Where(quote =>
                     !string.IsNullOrWhiteSpace(quote.Error)))
        {
            warnings.Add($"TME {quote.Mpn}: {quote.Error}");
        }

        var totals = CalculateTotals(lines);

        if (totals.MarketCoveragePercent < 100m)
        {
            warnings.Add(
                $"TME market-price coverage is {totals.MarketCoveragePercent:0.#}% of priced BOM lines; reference prices are used as fallback where available.");
        }

        return new ProjectPricingResponse
        {
            ProjectId = project.Id,
            ProjectName = project.Name ?? string.Empty,
            Country = _options.Country,
            Currency = _options.Currency,
            VatRate = _options.VatRate,
            GeneratedAtUtc = DateTime.UtcNow,
            SchemaMode = activeSchema.Mode,
            SchemaRevision = activeSchema.Revision,
            Totals = totals,
            Items = lines
                .OrderBy(line => CategoryOrder(line.Category))
                .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Warnings = warnings
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private List<CatalogCandidate> BuildCatalogCandidates(
        SchemaDocument schema,
        InitialSettings settings,
        List<string> warnings)
    {
        var candidates = new List<CatalogCandidate>();
        var components = schema.Components
            .Where(component =>
                string.Equals(component.Mounting, "din", StringComparison.Ordinal) &&
                component.Slots > 0)
            .ToList();

        foreach (var group in components.GroupBy(component =>
                     BuildEditorGroupingKey(component, settings)))
        {
            var sample = group.First();
            var catalog = SchneiderProductCatalog.ResolveEditorComponent(
                sample,
                settings);
            var description = DescribeEditorComponent(sample);
            var manualCount = group.Count(component =>
                string.Equals(component.Origin, "manual", StringComparison.Ordinal));

            if (catalog == null)
            {
                warnings.Add(
                    $"No exact Schneider reference is mapped yet for {description}. This line is excluded from the price until its product specification is completed.");
            }

            candidates.Add(new CatalogCandidate(
                "component",
                catalog?.Name ?? description,
                group.Count(),
                catalog,
                JoinNotes(
                    catalog?.ReferenceNote ??
                    "Exact Schneider product reference is not mapped yet.",
                    manualCount > 0
                        ? $"Includes {manualCount} manually placed component(s) from the customized schema."
                        : null)));
        }

        var rowCount = ActiveRailCount(schema);
        var enclosure = SchneiderProductCatalog.ResolveEnclosure(
            settings.ShieldWidth,
            rowCount);

        if (enclosure == null)
        {
            warnings.Add(
                $"No exact Schneider enclosure is mapped yet for {rowCount} row(s) × {settings.ShieldWidth} modules. Enclosure cost is currently excluded.");

            candidates.Add(new CatalogCandidate(
                "enclosure",
                $"Schneider enclosure {rowCount}×{settings.ShieldWidth} modules",
                1,
                null,
                "Exact enclosure reference is not mapped yet."));
        }
        else
        {
            candidates.Add(new CatalogCandidate(
                "enclosure",
                enclosure.Name,
                1,
                enclosure,
                enclosure.ReferenceNote));
        }

        return candidates;
    }

    private void AddWireLines(
        SchemaDocument schema,
        ICollection<PricingLine> lines,
        ICollection<string> warnings)
    {
        var entries = new List<WireBomEntry>();

        foreach (var connection in schema.Connections)
        {
            entries.Add(CreateWireEntry(
                connection.Conductor,
                ColourForConductor(connection.Conductor),
                connection.SectionMm2,
                connection.LengthMeters,
                connection.LengthSource,
                legacy: false));
        }

        foreach (var connection in schema.UnresolvedConnections)
        {
            var conductor = ConductorFromColour(connection.Colour);
            entries.Add(CreateWireEntry(
                conductor ?? "Unknown",
                connection.Colour ?? "Unknown",
                connection.SectionMm2,
                connection.LengthMeters,
                connection.LengthSource,
                legacy: true));
        }

        if (entries.Count == 0)
            return;

        foreach (var group in entries.GroupBy(entry => new
                 {
                     entry.Conductor,
                     entry.Colour,
                     entry.SectionMm2,
                     entry.LengthSource
                 }))
        {
            var length = Math.Round(
                group.Sum(entry => entry.LengthMeters),
                2,
                MidpointRounding.AwayFromZero);
            var unitPrice = _options.WirePricePerMeterNetEur;
            var total = Money(length * unitPrice);
            var legacyCount = group.Count(entry => entry.Legacy);
            var sourceNote = group.Key.LengthSource switch
            {
                "manual" => "Length is stored as a manual editor value.",
                "calculated" => "Length is stored as calculated routing geometry.",
                _ =>
                    $"Estimated from {group.Count()} connection(s): {_options.WireMetersPerConnection:0.##} m/connection × {_options.WireWasteFactor:0.##} waste factor."
            };

            lines.Add(new PricingLine
            {
                Category = "wire",
                Name =
                    $"Internal panel wire {group.Key.Conductor} {group.Key.Colour}, " +
                    $"{FormatSection(group.Key.SectionMm2)} mm²",
                Manufacturer = null,
                Mpn = null,
                Quantity = length,
                Unit = "m",
                MarketUnitNet = null,
                ReferenceUnitNet = Money(unitPrice),
                MarketTotalNet = null,
                ReferenceTotalNet = total,
                RangeMinTotalNet = total,
                RangeMaxTotalNet = total,
                PriceSource = "configured-wire-allowance",
                Estimated = string.Equals(
                    group.Key.LengthSource,
                    "estimated",
                    StringComparison.Ordinal),
                Conductor = group.Key.Conductor,
                SectionMm2 = group.Key.SectionMm2,
                LengthSource = group.Key.LengthSource,
                Note = JoinNotes(
                    sourceNote,
                    legacyCount > 0
                        ? $"Includes {legacyCount} legacy connection(s) without stable terminal mapping."
                        : null)
            });
        }

        if (entries.Any(entry => entry.LengthSource == "estimated"))
        {
            warnings.Add(
                "Some internal panel wire lengths are estimated. Enter a wire length in the connection inspector to replace the estimate for that connection.");
        }

        if (entries.Any(entry => entry.Legacy))
        {
            warnings.Add(
                "Legacy connections without stable terminal IDs are still included in the wire BOM using their available colour/section metadata.");
        }

        if (entries.Any(entry => !entry.SectionMm2.HasValue))
        {
            warnings.Add(
                "Some wire sections are not specified. Those BOM rows keep the configured generic wire allowance and are not an engineering cable-selection recommendation.");
        }

        if (entries.Any(entry => entry.SectionMm2 == 10m))
        {
            warnings.Add(
                "The legacy schema generator may store 10 mm² as a placeholder for internal wiring. Wire section in this BOM is therefore not an engineering recommendation unless it was reviewed in the editor.");
        }
    }

    private WireBomEntry CreateWireEntry(
        string conductor,
        string colour,
        decimal? sectionMm2,
        decimal? lengthMeters,
        string? lengthSource,
        bool legacy)
    {
        if (lengthMeters.HasValue)
        {
            return new WireBomEntry(
                conductor,
                colour,
                sectionMm2,
                Math.Max(0m, lengthMeters.Value),
                NormalizeStoredLengthSource(lengthSource),
                legacy);
        }

        return new WireBomEntry(
            conductor,
            colour,
            sectionMm2,
            _options.WireMetersPerConnection * _options.WireWasteFactor,
            "estimated",
            legacy);
    }

    private PricingLine CreateCatalogLine(
        string category,
        SchneiderCatalogItem catalog,
        decimal quantity,
        decimal? marketUnit,
        TmeCachedQuote? quote,
        string? extraNote)
    {
        var referenceUnit = catalog.ReferenceUnitNetEur;
        decimal? marketTotal = marketUnit.HasValue
            ? Money(marketUnit.Value * quantity)
            : null;
        decimal? referenceTotal = referenceUnit.HasValue
            ? Money(referenceUnit.Value * quantity)
            : null;

        var knownTotals = new[] { marketTotal, referenceTotal }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        return new PricingLine
        {
            Category = category,
            Name = catalog.Name,
            Manufacturer = "Schneider Electric",
            Mpn = catalog.Mpn,
            TmeSymbol = quote?.Symbol,
            Quantity = quantity,
            Unit = "pcs",
            MarketUnitNet = marketUnit.HasValue
                ? Money(marketUnit.Value)
                : null,
            ReferenceUnitNet = referenceUnit.HasValue
                ? Money(referenceUnit.Value)
                : null,
            MarketTotalNet = marketTotal,
            ReferenceTotalNet = referenceTotal,
            RangeMinTotalNet = knownTotals.Length == 0
                ? null
                : knownTotals.Min(),
            RangeMaxTotalNet = knownTotals.Length == 0
                ? null
                : knownTotals.Max(),
            StockQuantity = quote?.StockQuantity,
            PriceSource = marketUnit.HasValue
                ? "TME Germany + Schneider reference"
                : referenceUnit.HasValue
                    ? "Schneider reference fallback"
                    : "unavailable",
            PriceUpdatedAtUtc = quote?.FetchedAtUtc,
            Estimated = false,
            Note = JoinNotes(
                extraNote,
                marketUnit.HasValue
                    ? "Market price from TME for Germany/EUR."
                    : quote?.Error)
        };
    }

    private PricingTotals CalculateTotals(IReadOnlyCollection<PricingLine> lines)
    {
        decimal purchaseEstimate = 0;
        decimal reference = 0;
        decimal rangeMin = 0;
        decimal rangeMax = 0;
        var hasReference = false;
        var hasRange = false;
        var unpriced = 0;
        var pricedLines = 0;
        var marketEligibleLines = 0;
        var marketPricedLines = 0;

        foreach (var line in lines)
        {
            var fallbackTotal = line.MarketTotalNet ?? line.ReferenceTotalNet;

            if (fallbackTotal.HasValue)
            {
                purchaseEstimate += fallbackTotal.Value;
                pricedLines++;
            }
            else
            {
                unpriced++;
            }

            if (!line.Category.Equals("wire", StringComparison.OrdinalIgnoreCase) &&
                fallbackTotal.HasValue)
            {
                marketEligibleLines++;

                if (line.MarketTotalNet.HasValue)
                    marketPricedLines++;
            }

            if (line.ReferenceTotalNet.HasValue)
            {
                reference += line.ReferenceTotalNet.Value;
                hasReference = true;
            }

            if (line.RangeMinTotalNet.HasValue &&
                line.RangeMaxTotalNet.HasValue)
            {
                rangeMin += line.RangeMinTotalNet.Value;
                rangeMax += line.RangeMaxTotalNet.Value;
                hasRange = true;
            }
        }

        var vatMultiplier = 1m + _options.VatRate / 100m;
        var coverage = marketEligibleLines == 0
            ? 0m
            : Math.Round(
                (decimal)marketPricedLines / marketEligibleLines * 100m,
                1,
                MidpointRounding.AwayFromZero);

        return new PricingTotals
        {
            PurchaseEstimateNet = Money(purchaseEstimate),
            ReferenceNet = hasReference ? Money(reference) : null,
            RangeMinNet = hasRange ? Money(rangeMin) : null,
            RangeMaxNet = hasRange ? Money(rangeMax) : null,
            PurchaseEstimateGross = Money(purchaseEstimate * vatMultiplier),
            ReferenceGross = hasReference
                ? Money(reference * vatMultiplier)
                : null,
            RangeMinGross = hasRange
                ? Money(rangeMin * vatMultiplier)
                : null,
            RangeMaxGross = hasRange
                ? Money(rangeMax * vatMultiplier)
                : null,
            MarketCoveragePercent = coverage,
            UnpricedItemCount = unpriced,
            MarketPricedLineCount = marketPricedLines,
            TotalPricedLineCount = pricedLines
        };
    }

    private static string BuildEditorGroupingKey(
        EditorComponent component,
        InitialSettings settings)
    {
        var type = component.CatalogTypeId ?? "unknown";

        return type switch
        {
            "fusebox.av" => $"{type}:{component.Amperage:0.###}",
            "fusebox.rcd" => $"{type}:{component.RcdMilliAmps}",
            "fusebox.introductory" or
            "fusebox.introductory3p" or
            "fusebox.introductory3pn" => $"{type}:{settings.MainAmperage}",
            _ => type
        };
    }

    private static string DescribeEditorComponent(EditorComponent component)
    {
        var catalog = string.IsNullOrWhiteSpace(component.CatalogTypeId)
            ? null
            : ComponentCatalog.ResolveById(component.CatalogTypeId);
        var name = catalog?.LegacyName ?? component.CatalogTypeId ?? "Component";

        if (string.Equals(name, "av", StringComparison.OrdinalIgnoreCase))
        {
            return component.Amperage.HasValue
                ? $"Circuit breaker C{component.Amperage.Value:0}"
                : "Circuit breaker";
        }

        if (string.Equals(name, "rcd", StringComparison.OrdinalIgnoreCase))
        {
            return component.RcdMilliAmps.HasValue
                ? $"RCD {component.RcdMilliAmps.Value}mA"
                : "RCD";
        }

        return name;
    }

    private static int ActiveRailCount(SchemaDocument schema)
    {
        var rows = schema.Components
            .Where(component => string.Equals(
                component.Mounting,
                "din",
                StringComparison.Ordinal))
            .Select(component => component.RowIndex)
            .Concat(schema.ReservedSlots.Select(slot => slot.RowIndex))
            .Where(row => row >= 0)
            .ToArray();

        return rows.Length == 0 ? 1 : rows.Max() + 1;
    }

    private static string NormalizeStoredLengthSource(string? source) =>
        source is "manual" or "calculated" or "estimated"
            ? source
            : "manual";

    private static string? ConductorFromColour(string? colour) =>
        colour?.Trim().ToLowerInvariant() switch
        {
            "red" => "L1",
            "orange" => "L2",
            "grey" or "gray" => "L3",
            "blue" => "N",
            "green/yellow" or "green-yellow" or "greenyellow" => "PE",
            _ => null
        };

    private static string ColourForConductor(string conductor) =>
        conductor switch
        {
            "L1" => "red",
            "L2" => "orange",
            "L3" => "grey",
            "N" => "blue",
            "PE" => "green/yellow",
            _ => "unknown"
        };

    private static string FormatSection(decimal? section) =>
        section.HasValue
            ? section.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "?";

    private static string? JoinNotes(params string?[] values)
    {
        var parts = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parts.Length == 0 ? null : string.Join(" ", parts);
    }

    private static decimal Money(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static int CategoryOrder(string category) =>
        category switch
        {
            "component" => 0,
            "enclosure" => 1,
            "wire" => 2,
            _ => 9
        };

    private sealed record CatalogCandidate(
        string Category,
        string Name,
        decimal Quantity,
        SchneiderCatalogItem? Catalog,
        string? Note);

    private sealed record WireBomEntry(
        string Conductor,
        string Colour,
        decimal? SectionMm2,
        decimal LengthMeters,
        string LengthSource,
        bool Legacy);
}
