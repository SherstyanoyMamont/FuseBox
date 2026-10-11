using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Services.Pricing;

public sealed class TmePriceCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public TmePriceCacheStore(
        IHostEnvironment environment,
        IOptions<PricingOptions> options)
    {
        var configured = options.Value.CacheFile?.Trim();

        _path = Path.IsPathRooted(configured)
            ? configured!
            : Path.Combine(
                environment.ContentRootPath,
                string.IsNullOrWhiteSpace(configured)
                    ? "App_Data/tme-price-cache.json"
                    : configured!);
    }

    public async Task<Dictionary<string, TmeCachedQuote>> ReadAsync(
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MergeAsync(
        IEnumerable<TmeCachedQuote> quotes,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var cache = await ReadUnsafeAsync(cancellationToken);

            foreach (var quote in quotes)
            {
                cache[BuildStorageKey(
                    quote.Country,
                    quote.Currency,
                    quote.Mpn)] = quote;
            }

            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var tempPath = _path + ".tmp";
            var json = JsonSerializer.Serialize(cache, JsonOptions);
            await File.WriteAllTextAsync(
                tempPath,
                json,
                cancellationToken);

            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, TmeCachedQuote>> ReadUnsafeAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return new(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = await File.ReadAllTextAsync(
                _path,
                cancellationToken);

            return JsonSerializer.Deserialize<Dictionary<string, TmeCachedQuote>>(
                       json,
                       JsonOptions)
                   ?? new(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            // Corrupt cache must never break panel pricing. Start fresh.
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string NormalizeKey(string mpn) =>
        mpn.Trim().ToUpperInvariant();

    public static string BuildStorageKey(
        string country,
        string currency,
        string mpn) =>
        $"{country.Trim().ToUpperInvariant()}|{currency.Trim().ToUpperInvariant()}|{NormalizeKey(mpn)}";
}
