using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Services.Pricing;

public sealed class TmeApiClient
{
    private readonly HttpClient _http;
    private readonly TmePriceCacheStore _cacheStore;
    private readonly PricingOptions _options;
    private readonly ILogger<TmeApiClient> _logger;
    private readonly SemaphoreSlim _tokenGate = new(1, 1);

    private string? _accessToken;
    private DateTime _accessTokenExpiresAtUtc;

    public TmeApiClient(
        HttpClient http,
        TmePriceCacheStore cacheStore,
        IOptions<PricingOptions> options,
        ILogger<TmeApiClient> logger)
    {
        _http = http;
        _cacheStore = cacheStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, TmeCachedQuote>>
        GetQuotesAsync(
            IEnumerable<string> mpns,
            bool forceRefresh,
            CancellationToken cancellationToken)
    {
        var requested = mpns
            .Where(mpn => !string.IsNullOrWhiteSpace(mpn))
            .Select(TmePriceCacheStore.NormalizeKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var result = new Dictionary<string, TmeCachedQuote>(
            StringComparer.OrdinalIgnoreCase);

        if (requested.Length == 0)
            return result;

        var cache = await _cacheStore.ReadAsync(cancellationToken);
        var cutoff = DateTime.UtcNow.AddHours(-Math.Max(1, _options.CacheHours));
        var missing = new List<string>();

        foreach (var mpn in requested)
        {
            var storageKey = TmePriceCacheStore.BuildStorageKey(
                _options.Country,
                _options.Currency,
                mpn);

            if (!forceRefresh &&
                cache.TryGetValue(storageKey, out var cached) &&
                cached.FetchedAtUtc >= cutoff)
            {
                result[mpn] = cached;
            }
            else
            {
                missing.Add(mpn);
            }
        }

        if (missing.Count == 0)
            return result;

        if (string.IsNullOrWhiteSpace(_options.Tme.Token) ||
            string.IsNullOrWhiteSpace(_options.Tme.Secret))
        {
            foreach (var mpn in missing)
            {
                var storageKey = TmePriceCacheStore.BuildStorageKey(
                    _options.Country,
                    _options.Currency,
                    mpn);

                if (cache.TryGetValue(storageKey, out var previous))
                {
                    previous.Error =
                        "TME credentials are not configured; showing the last cached quote.";
                    result[mpn] = previous;
                }
                else
                {
                    result[mpn] = new TmeCachedQuote
                    {
                        Country = _options.Country,
                        Currency = _options.Currency,
                        Mpn = mpn,
                        FetchedAtUtc = DateTime.UtcNow,
                        Error = "TME credentials are not configured."
                    };
                }
            }

            return result;
        }

        foreach (var batch in missing.Chunk(50))
        {
            IReadOnlyList<TmeCachedQuote> fresh;

            try
            {
                fresh = await FetchBatchAsync(batch, cancellationToken);
            }
            catch (Exception exception) when (
                exception is HttpRequestException or
                TaskCanceledException or
                InvalidOperationException)
            {
                _logger.LogWarning(
                    exception,
                    "TME price refresh failed for {Count} MPNs.",
                    batch.Length);

                fresh = batch.Select(mpn =>
                {
                    var storageKey = TmePriceCacheStore.BuildStorageKey(
                        _options.Country,
                        _options.Currency,
                        mpn);

                    if (cache.TryGetValue(storageKey, out var previous))
                    {
                        previous.Error =
                            "TME refresh failed; showing the last cached quote.";
                        return previous;
                    }

                    return new TmeCachedQuote
                    {
                        Country = _options.Country,
                        Currency = _options.Currency,
                        Mpn = mpn,
                        FetchedAtUtc = DateTime.UtcNow,
                        Error = "TME is temporarily unavailable."
                    };
                }).ToArray();
            }

            await _cacheStore.MergeAsync(fresh, cancellationToken);

            foreach (var quote in fresh)
                result[TmePriceCacheStore.NormalizeKey(quote.Mpn)] = quote;
        }

        return result;
    }

    private async Task<IReadOnlyList<TmeCachedQuote>> FetchBatchAsync(
        IReadOnlyCollection<string> mpns,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var productRoot = await GetJsonAsync(
            BuildProductsUrl(mpns),
            token,
            cancellationToken);

        var products = GetElements(productRoot);
        var byMpn = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var product in products.OfType<JsonObject>())
        {
            var symbol = GetString(product, "symbol");
            if (string.IsNullOrWhiteSpace(symbol))
                continue;

            var manufacturer = product["manufacturer"] as JsonObject;
            var manufacturerName = manufacturer == null
                ? null
                : GetString(manufacturer, "name");

            // When the manufacturer field is present, prefer Schneider only.
            if (!string.IsNullOrWhiteSpace(manufacturerName) &&
                !manufacturerName.Contains(
                    "Schneider",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var manufacturerSymbols = product["manufacturer_symbols"] as JsonArray;
            if (manufacturerSymbols == null)
                continue;

            foreach (var symbolNode in manufacturerSymbols)
            {
                var manufacturerSymbol = symbolNode?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(manufacturerSymbol))
                    continue;

                var normalized = TmePriceCacheStore.NormalizeKey(
                    manufacturerSymbol);

                if (mpns.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    byMpn[normalized] = symbol;
            }
        }

        var resolvedSymbols = byMpn.Values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var dataBySymbol = new Dictionary<string, JsonObject>(
            StringComparer.OrdinalIgnoreCase);

        if (resolvedSymbols.Length > 0)
        {
            var dataRoot = await GetJsonAsync(
                BuildProductDataUrl(resolvedSymbols),
                token,
                cancellationToken);

            foreach (var dataItem in GetElements(dataRoot).OfType<JsonObject>())
            {
                var symbol = GetString(dataItem, "symbol");
                if (!string.IsNullOrWhiteSpace(symbol))
                    dataBySymbol[symbol] = dataItem;
            }
        }

        var now = DateTime.UtcNow;
        var quotes = new List<TmeCachedQuote>(mpns.Count);

        foreach (var mpn in mpns)
        {
            if (!byMpn.TryGetValue(mpn, out var symbol))
            {
                quotes.Add(new TmeCachedQuote
                {
                    Country = _options.Country,
                    Currency = _options.Currency,
                    Mpn = mpn,
                    FetchedAtUtc = now,
                    Error = $"MPN was not found in the TME {_options.Country} catalog."
                });
                continue;
            }

            if (!dataBySymbol.TryGetValue(symbol, out var dataItem))
            {
                quotes.Add(new TmeCachedQuote
                {
                    Country = _options.Country,
                    Currency = _options.Currency,
                    Mpn = mpn,
                    Symbol = symbol,
                    FetchedAtUtc = now,
                    Error = "TME returned no price data for the product."
                });
                continue;
            }

            quotes.Add(ParseQuote(mpn, symbol, dataItem, now));
        }

        return quotes;
    }

    private TmeCachedQuote ParseQuote(
        string mpn,
        string symbol,
        JsonObject dataItem,
        DateTime now)
    {
        var quote = new TmeCachedQuote
        {
            Country = _options.Country,
            Currency = _options.Currency,
            Mpn = mpn,
            Symbol = symbol,
            StockQuantity = GetInt(dataItem, "stock_quantity"),
            FetchedAtUtc = now
        };

        if (dataItem["prices"] is not JsonObject prices)
        {
            quote.Error = "TME returned no prices object.";
            return quote;
        }

        var priceType = GetString(prices, "type")?.ToUpperInvariant();
        var taxRate = 0m;

        if (prices["tax"] is JsonObject tax)
            taxRate = GetDecimal(tax, "rate") ?? 0m;

        if (prices["elements"] is not JsonArray tiers)
        {
            quote.Error = "TME returned no price tiers.";
            return quote;
        }

        foreach (var tierNode in tiers.OfType<JsonObject>())
        {
            var amount = GetDecimal(tierNode, "amount");
            var price = GetDecimal(tierNode, "price");

            if (!amount.HasValue || !price.HasValue || amount <= 0 || price < 0)
                continue;

            var net = price.Value;
            if (priceType == "GROSS" && taxRate > 0)
                net /= 1m + taxRate / 100m;

            quote.PriceTiers.Add(new TmePriceTier
            {
                Amount = amount.Value,
                UnitNet = Math.Round(net, 4, MidpointRounding.AwayFromZero)
            });
        }

        quote.PriceTiers = quote.PriceTiers
            .OrderBy(tier => tier.Amount)
            .ToList();

        if (quote.PriceTiers.Count == 0)
            quote.Error = "TME returned no usable price tier.";

        return quote;
    }

    private async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) &&
            _accessTokenExpiresAtUtc > DateTime.UtcNow.AddSeconds(20))
        {
            return _accessToken;
        }

        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) &&
                _accessTokenExpiresAtUtc > DateTime.UtcNow.AddSeconds(20))
            {
                return _accessToken;
            }

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    $"{_options.Tme.Token}:{_options.Tme.Secret}"));

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "auth/token");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            request.Content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials"
                });

            using var response = await _http.SendAsync(
                request,
                cancellationToken);

            var text = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"TME token request failed with HTTP {(int)response.StatusCode}.");
            }

            var root = JsonNode.Parse(text) as JsonObject
                ?? throw new InvalidOperationException(
                    "TME token response is not valid JSON.");

            var token = GetString(root, "access_token");
            var expiresIn = GetInt(root, "expires_in") ?? 300;

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    "TME token response does not contain access_token.");
            }

            _accessToken = token;
            _accessTokenExpiresAtUtc = DateTime.UtcNow
                .AddSeconds(Math.Max(60, expiresIn));

            return token;
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    private async Task<JsonObject> GetJsonAsync(
        string relativeUrl,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            relativeUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptLanguage.ParseAdd("en");

        // TME credentials linked to a customer account may return the
        // customer-specific price list. For FuseBox we need a neutral public
        // market observation, so request anonymous market context explicitly.
        // See the official TME API setup guide.
        request.Headers.TryAddWithoutValidation(
            "request-context",
            "anonymous");

        using var response = await _http.SendAsync(
            request,
            cancellationToken);

        var text = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"TME API request failed with HTTP {(int)response.StatusCode}.");
        }

        var root = JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException(
                "TME API response is not valid JSON.");

        var status = GetString(root, "status");
        if (!string.IsNullOrWhiteSpace(status) &&
            !status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"TME API returned status {status}.");
        }

        return root;
    }

    private string BuildProductsUrl(IEnumerable<string> mpns)
    {
        var query = new List<string>
        {
            $"country={Uri.EscapeDataString(_options.Country)}"
        };

        query.AddRange(mpns.Select(mpn =>
            $"mpns[]={Uri.EscapeDataString(mpn)}"));

        return "products?" + string.Join("&", query);
    }

    private string BuildProductDataUrl(IEnumerable<string> symbols)
    {
        var query = new List<string>
        {
            $"country={Uri.EscapeDataString(_options.Country)}",
            $"currency={Uri.EscapeDataString(_options.Currency)}",
            "scope[]=prices",
            "scope[]=stock"
        };

        query.AddRange(symbols.Select(symbol =>
            $"symbols[]={Uri.EscapeDataString(symbol)}"));

        return "products/data?" + string.Join("&", query);
    }

    private static JsonArray GetElements(JsonObject root) =>
        root["data"]?["elements"] as JsonArray ?? new JsonArray();

    private static string? GetString(JsonObject value, string key)
    {
        if (value[key] is JsonValue node &&
            node.TryGetValue<string>(out var text))
        {
            return text;
        }

        return null;
    }

    private static int? GetInt(JsonObject value, string key)
    {
        if (value[key] is JsonValue node)
        {
            if (node.TryGetValue<int>(out var integer))
                return integer;
            if (node.TryGetValue<long>(out var longValue) &&
                longValue is >= int.MinValue and <= int.MaxValue)
                return (int)longValue;
        }

        return null;
    }

    private static decimal? GetDecimal(JsonObject value, string key)
    {
        if (value[key] is JsonValue node)
        {
            if (node.TryGetValue<decimal>(out var decimalValue))
                return decimalValue;
            if (node.TryGetValue<double>(out var doubleValue) &&
                double.IsFinite(doubleValue))
                return (decimal)doubleValue;
            if (node.TryGetValue<int>(out var intValue))
                return intValue;
        }

        return null;
    }
}
