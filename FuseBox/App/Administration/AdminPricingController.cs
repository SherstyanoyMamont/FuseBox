using FuseBox.App.Catalog;
using FuseBox.App.DataBase;
using FuseBox.App.Services.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Administration;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/pricing")]
public sealed class AdminPricingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TmeApiClient _tme;

    public AdminPricingController(AppDbContext context, TmeApiClient tme)
    {
        _context = context;
        _tme = tme;
    }

    [HttpPost("refresh")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh(
        CancellationToken cancellationToken)
    {
        var managedMpns = await _context.ManagedComponentCatalogItems
            .AsNoTracking()
            .Where(item => !item.IsDeleted && item.Mpn != null)
            .Select(item => new { item.Mpn, item.Manufacturer })
            .ToArrayAsync(cancellationToken);

        var mpns = SchneiderProductCatalog.GetKnownMpns()
            .Concat(managedMpns
                .Where(item => item.Manufacturer != null &&
                               item.Manufacturer.Contains(
                                   "Schneider",
                                   StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Mpn!))
            .Where(mpn => !string.IsNullOrWhiteSpace(mpn))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var quotes = await _tme.GetQuotesAsync(
            mpns,
            forceRefresh: true,
            cancellationToken: cancellationToken);

        var priced = quotes.Values.Count(quote => quote.HasMarketPrice);
        var notFound = quotes.Values.Count(quote =>
            quote.Error?.StartsWith(
                "MPN was not found",
                StringComparison.OrdinalIgnoreCase) == true);
        var failed = quotes.Values.Count(quote =>
            !quote.HasMarketPrice &&
            !string.IsNullOrWhiteSpace(quote.Error) &&
            !quote.Error.StartsWith(
                "MPN was not found",
                StringComparison.OrdinalIgnoreCase));

        return Ok(new
        {
            country = quotes.Values.FirstOrDefault()?.Country ?? "DE",
            currency = quotes.Values.FirstOrDefault()?.Currency ?? "EUR",
            requested = mpns.Length,
            priced,
            notFound,
            failed,
            fetchedAtUtc = quotes.Values
                .Select(quote => quote.FetchedAtUtc)
                .DefaultIfEmpty(DateTime.UtcNow)
                .Max()
        });
    }
}
