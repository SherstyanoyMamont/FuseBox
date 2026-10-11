using System.Text.Json.Serialization;

namespace FuseBox.App.Services.Pricing;

public sealed class ProjectPricingResponse
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string Country { get; init; } = "DE";
    public string Currency { get; init; } = "EUR";
    public decimal VatRate { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public PricingTotals Totals { get; init; } = new();
    public List<PricingLine> Items { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}

public sealed class PricingTotals
{
    // Purchase estimate uses a fresh market price when available and falls
    // back to the reference price for an otherwise priced item.
    public decimal PurchaseEstimateNet { get; init; }
    public decimal? ReferenceNet { get; init; }
    public decimal? RangeMinNet { get; init; }
    public decimal? RangeMaxNet { get; init; }

    public decimal PurchaseEstimateGross { get; init; }
    public decimal? ReferenceGross { get; init; }
    public decimal? RangeMinGross { get; init; }
    public decimal? RangeMaxGross { get; init; }

    public decimal MarketCoveragePercent { get; init; }
    public int UnpricedItemCount { get; init; }
    public int MarketPricedLineCount { get; init; }
    public int TotalPricedLineCount { get; init; }
}

public sealed class PricingLine
{
    public string Category { get; init; } = "component";
    public string Name { get; init; } = string.Empty;
    public string? Manufacturer { get; init; }
    public string? Mpn { get; init; }
    public string? TmeSymbol { get; init; }

    public decimal Quantity { get; init; }
    public string Unit { get; init; } = "pcs";

    public decimal? MarketUnitNet { get; init; }
    public decimal? ReferenceUnitNet { get; init; }
    public decimal? MarketTotalNet { get; init; }
    public decimal? ReferenceTotalNet { get; init; }
    public decimal? RangeMinTotalNet { get; init; }
    public decimal? RangeMaxTotalNet { get; init; }

    public int? StockQuantity { get; init; }
    public string PriceSource { get; init; } = "unavailable";
    public DateTime? PriceUpdatedAtUtc { get; init; }
    public bool Estimated { get; init; }
    public string? Note { get; init; }
}

public sealed class TmePriceTier
{
    public decimal Amount { get; set; }
    public decimal UnitNet { get; set; }
}

public sealed class TmeCachedQuote
{
    public string Country { get; set; } = "DE";
    public string Currency { get; set; } = "EUR";
    public string Mpn { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public List<TmePriceTier> PriceTiers { get; set; } = new();
    public int? StockQuantity { get; set; }
    public DateTime FetchedAtUtc { get; set; }
    public string? Error { get; set; }

    [JsonIgnore]
    public bool HasMarketPrice => PriceTiers.Count > 0;

    public decimal? GetUnitNetForQuantity(decimal quantity)
    {
        if (PriceTiers.Count == 0)
            return null;

        var eligible = PriceTiers
            .Where(tier => tier.Amount <= quantity)
            .OrderByDescending(tier => tier.Amount)
            .FirstOrDefault();

        return (eligible ?? PriceTiers.OrderBy(tier => tier.Amount).First())
            .UnitNet;
    }
}

internal sealed record SchneiderCatalogItem(
    string Name,
    string Mpn,
    decimal? ReferenceUnitNetEur,
    string ReferenceNote);
