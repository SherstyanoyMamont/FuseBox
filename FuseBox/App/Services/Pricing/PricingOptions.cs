namespace FuseBox.App.Services.Pricing;

public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    public string Country { get; set; } = "DE";
    public string Currency { get; set; } = "EUR";
    public decimal VatRate { get; set; } = 19m;
    public int CacheHours { get; set; } = 24;

    // First-stage estimate for internal panel wiring. The current schema stores
    // connectivity, but it does not store a measured physical wire length.
    public decimal WireMetersPerConnection { get; set; } = 0.45m;
    public decimal WireWasteFactor { get; set; } = 1.15m;
    public decimal WirePricePerMeterNetEur { get; set; } = 3.50m;

    public string CacheFile { get; set; } =
        "App_Data/tme-price-cache.json";

    public TmePricingOptions Tme { get; set; } = new();
}

public sealed class TmePricingOptions
{
    public string BaseUrl { get; set; } = "https://api.tme.eu/";

    // Configure these with User Secrets or environment variables.
    // Pricing:Tme:Token
    // Pricing:Tme:Secret
    public string? Token { get; set; }
    public string? Secret { get; set; }
}
