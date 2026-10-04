using global::FuseBox;
using FuseBox.App.Models.Shild_Comp;
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
        Project schemaProject,
        InitialSettings settings,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        if (schemaProject.FuseBox == null)
            throw new InvalidOperationException("Project fuse box is missing.");

        var warnings = new List<string>
        {
            "Schneider reference prices are static public list-price snapshots in this first stage. TME Germany is the live market source; a live Schneider provider can replace the snapshots later."
        };
        var candidates = BuildCatalogCandidates(
            schemaProject,
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

        AddWireLines(schemaProject, lines, warnings);

        foreach (var quote in quotes.Values.Where(quote =>
                     !string.IsNullOrWhiteSpace(quote.Error)))
        {
            warnings.Add(
                $"TME {quote.Mpn}: {quote.Error}");
        }

        var totals = CalculateTotals(lines);

        if (totals.MarketCoveragePercent < 100m)
        {
            warnings.Add(
                $"TME market-price coverage is {totals.MarketCoveragePercent:0.#}% of priced BOM lines; reference prices are used as fallback where available.");
        }

        return new ProjectPricingResponse
        {
            ProjectId = schemaProject.Id,
            ProjectName = schemaProject.Name ?? string.Empty,
            Country = _options.Country,
            Currency = _options.Currency,
            VatRate = _options.VatRate,
            GeneratedAtUtc = DateTime.UtcNow,
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
        Project project,
        InitialSettings settings,
        List<string> warnings)
    {
        var candidates = new List<CatalogCandidate>();
        var components = project.FuseBox.ComponentGroups
            .SelectMany(group => group.Components)
            .Where(component =>
                component is not EmptySlot &&
                !IsStartPoint(component))
            .ToList();

        foreach (var group in components.GroupBy(component =>
                     BuildComponentGroupingKey(component, settings)))
        {
            var sample = group.First();
            var catalog = SchneiderProductCatalog.ResolveComponent(
                sample,
                settings);

            if (catalog == null)
            {
                warnings.Add(
                    $"No exact Schneider reference is mapped yet for {DescribeComponent(sample)}. This line is excluded from the price until its product specification is completed.");
            }

            candidates.Add(new CatalogCandidate(
                "component",
                catalog?.Name ?? DescribeComponent(sample),
                group.Count(),
                catalog,
                catalog?.ReferenceNote ??
                "Exact Schneider product reference is not mapped yet."));
        }

        var rowCount = Math.Max(
            1,
            project.FuseBox.ComponentGroups.Count(group =>
                group.Components.Any(component => component is not EmptySlot)));
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
        Project project,
        ICollection<PricingLine> lines,
        ICollection<string> warnings)
    {
        var connections = project.FuseBox.CableConnections
            .Where(connection => connection.Cable != null)
            .ToList();

        if (connections.Count == 0)
            return;

        foreach (var group in connections.GroupBy(connection => new
                 {
                     Colour = connection.Cable.Сolour?.Trim() ?? "Unknown",
                     Section = connection.Cable.Section
                 }))
        {
            var rawLength = group.Count() *
                            _options.WireMetersPerConnection *
                            _options.WireWasteFactor;

            var length = Math.Ceiling(rawLength * 10m) / 10m;
            var unitPrice = _options.WirePricePerMeterNetEur;
            var total = Money(length * unitPrice);

            lines.Add(new PricingLine
            {
                Category = "wire",
                Name = $"Internal panel wire {group.Key.Colour}, {FormatSection(group.Key.Section)} mm²",
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
                Estimated = true,
                Note = $"Estimated from {group.Count()} electrical connection(s): {_options.WireMetersPerConnection:0.##} m/connection × {_options.WireWasteFactor:0.##} waste factor."
            });
        }

        warnings.Add(
            "Internal panel wire lengths are estimates because the current backend stores connectivity, not measured physical wire length.");

        if (connections.Any(connection => connection.Cable.Section == 10m))
        {
            warnings.Add(
                "The current schema generator stores 10 mm² as a placeholder for internal wiring. Wire section in this BOM is therefore not an engineering recommendation yet.");
        }

        warnings.Add(
            "PE/green-yellow conductor is not represented in the current CableConnections model and is not included in the wire estimate yet.");
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
        var marketTotal = marketUnit.HasValue
            ? Money(marketUnit.Value * quantity)
            : null;
        var referenceTotal = referenceUnit.HasValue
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

    private static string BuildComponentGroupingKey(
        Component component,
        InitialSettings settings)
    {
        var name = component.Name?.Replace(" ", string.Empty) ?? "Unknown";

        return name.Equals("AV", StringComparison.OrdinalIgnoreCase)
            ? $"AV:{component.Amper:0.###}"
            : name.Equals("RCD", StringComparison.OrdinalIgnoreCase) && component is RCD rcd
                ? $"RCD:{component.Amper:0.###}:{rcd.Capacity}"
                : name.StartsWith("Introductory", StringComparison.OrdinalIgnoreCase)
                    ? $"{name}:{settings.MainAmperage}"
                    : name;
    }

    private static string DescribeComponent(Component component)
    {
        var name = component.Name?.Replace(" ", string.Empty) ?? "Component";

        if (name.Equals("AV", StringComparison.OrdinalIgnoreCase))
            return $"Circuit breaker C{component.Amper:0}";

        if (name.Equals("RCD", StringComparison.OrdinalIgnoreCase) && component is RCD rcd)
            return $"RCD {component.Amper:0}A / {rcd.Capacity}mA";

        return name;
    }

    private static bool IsStartPoint(Component component)
    {
        var name = component.Name?.Replace(" ", string.Empty);
        return name is "StartPoint" or "StartPoint3p";
    }

    private static string FormatSection(decimal? section) =>
        section.HasValue
            ? section.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
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
}
