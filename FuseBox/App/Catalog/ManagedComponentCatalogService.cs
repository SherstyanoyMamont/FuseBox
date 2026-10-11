using FuseBox.App.Contracts.Catalog;
using FuseBox.App.DataBase;
using FuseBox.App.SchemaEditor;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Catalog;

public sealed class ManagedComponentCatalogService
{
    private readonly AppDbContext _context;

    public ManagedComponentCatalogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task EnsureBuiltInRowsAsync(CancellationToken cancellationToken)
    {
        var existingIdValues = await _context.ManagedComponentCatalogItems
            .AsNoTracking()
            .Select(item => item.CatalogTypeId)
            .ToArrayAsync(cancellationToken);
        var existingIds = existingIdValues.ToHashSet(StringComparer.Ordinal);

        var now = DateTime.UtcNow;
        var added = false;

        foreach (var entry in ComponentCatalog.BuiltInEntries
                     .Where(entry => entry.Mounting == "din" && entry.Slots > 0))
        {
            if (existingIds.Contains(entry.Id))
                continue;

            _context.ManagedComponentCatalogItems.Add(new ManagedComponentCatalogItem
            {
                CatalogTypeId = entry.Id,
                TemplateCatalogTypeId = entry.Id,
                IsBuiltIn = true,
                DisplayName = DefaultDisplayName(entry),
                Category = DefaultCategory(entry),
                IsActive = entry.Mounting == "din" && entry.Slots > 0,
                IsDeleted = false,
                SortOrder = DefaultSortOrder(entry),
                ModelScale = 1d,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            added = true;
        }

        if (added)
            await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshRuntimeAliasesAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.ManagedComponentCatalogItems
            .AsNoTracking()
            .Select(item => new
            {
                item.CatalogTypeId,
                item.TemplateCatalogTypeId,
                item.IsBuiltIn,
                item.IsActive,
                item.IsDeleted
            })
            .ToArrayAsync(cancellationToken);

        ComponentCatalog.ApplyManagedAliases(rows.Select(item =>
            new ManagedCatalogAlias(
                item.CatalogTypeId,
                item.TemplateCatalogTypeId,
                item.IsBuiltIn,
                item.IsActive && !item.IsDeleted)));
    }

    public async Task<IReadOnlyDictionary<string, ManagedComponentCatalogItem>>
        GetByCatalogTypeIdsAsync(
            IEnumerable<string?> catalogTypeIds,
            CancellationToken cancellationToken)
    {
        var ids = catalogTypeIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
            return new Dictionary<string, ManagedComponentCatalogItem>(StringComparer.Ordinal);

        var rows = await _context.ManagedComponentCatalogItems
            .AsNoTracking()
            .Where(item => ids.Contains(item.CatalogTypeId))
            .ToArrayAsync(cancellationToken);

        return rows.ToDictionary(
            item => item.CatalogTypeId,
            StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<ManagedComponentCatalogItem>>
        GetPublicItemsAsync(CancellationToken cancellationToken) =>
        await _context.ManagedComponentCatalogItems
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.DisplayName)
            .ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken);

    public static ComponentCatalogItemResponse ToResponse(
        ManagedComponentCatalogItem item,
        Func<string?, string?> publicUrl)
    {
        var template = ComponentCatalog.ResolveBuiltInById(
            item.TemplateCatalogTypeId);

        return new ComponentCatalogItemResponse
        {
            Id = item.Id,
            CatalogTypeId = item.CatalogTypeId,
            TemplateCatalogTypeId = item.TemplateCatalogTypeId,
            IsBuiltIn = item.IsBuiltIn,
            DisplayName = item.DisplayName,
            Category = item.Category,
            Description = item.Description,
            Manufacturer = item.Manufacturer,
            Mpn = item.Mpn,
            ReferencePriceEur = item.ReferencePriceEur,
            DatasheetUrl = item.DatasheetUrl,
            ImageUrl = VersionedAssetUrl(item.ImageUrl, item.UpdatedAtUtc, publicUrl),
            Model3dUrl = VersionedAssetUrl(item.Model3dUrl, item.UpdatedAtUtc, publicUrl),
            ModelScale = item.ModelScale,
            ModelRotationX = item.ModelRotationX,
            ModelRotationY = item.ModelRotationY,
            ModelRotationZ = item.ModelRotationZ,
            ModelOffsetX = item.ModelOffsetX,
            ModelOffsetY = item.ModelOffsetY,
            ModelOffsetZ = item.ModelOffsetZ,
            DefaultAmperage = item.DefaultAmperage,
            DefaultRcdMilliAmps = item.DefaultRcdMilliAmps,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive,
            IsDeleted = item.IsDeleted,
            Selectable = item.IsActive &&
                         !item.IsDeleted &&
                         template?.Mounting == "din" &&
                         template.Slots > 0,
            Role = template?.Role ?? string.Empty,
            Mounting = template?.Mounting ?? string.Empty,
            Slots = template?.Slots ?? 0,
            TerminalModel = template?.TerminalModel ?? string.Empty,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc
        };
    }

    private static string? VersionedAssetUrl(
        string? storedUrl,
        DateTime updatedAtUtc,
        Func<string?, string?> publicUrl)
    {
        var resolved = publicUrl(storedUrl);
        if (string.IsNullOrWhiteSpace(resolved) ||
            string.IsNullOrWhiteSpace(storedUrl) ||
            !storedUrl.StartsWith("/uploads/components/", StringComparison.Ordinal))
        {
            return resolved;
        }

        var separator = resolved.Contains('?') ? '&' : '?';
        return $"{resolved}{separator}v={updatedAtUtc.Ticks}";
    }

    public static string DefaultDisplayName(CatalogEntry entry)
    {
        return entry.LegacyName switch
        {
            "av" => "MCB",
            "rcd" => "RCD",
            "introductory" => "Main switch 2P",
            "introductory3p" => "Main switch 3P",
            "introductory3pn" => "Main switch 3P+N",
            "spd" => "SPD",
            "spd3" => "SPD 3P+N",
            "loadswitch" => "Load switch",
            "dinrailmeter" => "Energy meter",
            "dinrailmeter3p" => "Energy meter 3P",
            "rcdfire" => "Fire RCD",
            "rcdfire3p" => "Fire RCD 3P",
            "voltagerelay" => "Voltage relay",
            "voltagerelay1" => "Voltage relay L1",
            "voltagerelay2" => "Voltage relay L2",
            "voltagerelay3" => "Voltage relay L3",
            "voltagerelay3p" => "Voltage relay 3P",
            "dinrailsocket" => "DIN socket",
            "ndiscline" => "N disconnectable line",
            "modularcontactor" => "Modular contactor",
            "crossblock" => "Cross module",
            _ => Humanize(entry.LegacyName)
        };
    }

    public static string DefaultCategory(CatalogEntry entry) => entry.Role switch
    {
        "mcb" or "rcd" or "spd" => "Protection",
        "switch" or "contactor" or "relay" => "Switching",
        "meter" => "Measurement",
        "distribution" => "Distribution",
        "socket" => "Accessories",
        _ => "Other"
    };

    private static int DefaultSortOrder(CatalogEntry entry) => entry.Role switch
    {
        "mcb" => 100,
        "rcd" => 200,
        "spd" => 300,
        "switch" => 400,
        "contactor" => 500,
        "relay" => 600,
        "meter" => 700,
        "distribution" => 800,
        "socket" => 900,
        _ => 1000
    };

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Component";

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}

public sealed class ComponentCatalogBootstrapHostedService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _services;
    private readonly ILogger<ComponentCatalogBootstrapHostedService> _logger;

    public ComponentCatalogBootstrapHostedService(
        IServiceProvider services,
        ILogger<ComponentCatalogBootstrapHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Load custom electrical aliases before the web host starts accepting
        // requests. Otherwise an old project containing a custom catalogTypeId
        // could briefly look unknown after a process restart.
        await InitializeAndRefreshAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Managed component catalog background refresh failed.");
            }
        }
    }

    private async Task InitializeAndRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var service = scope.ServiceProvider
                .GetRequiredService<ManagedComponentCatalogService>();

            await service.EnsureBuiltInRowsAsync(cancellationToken);
            await service.RefreshRuntimeAliasesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(
                exception,
                "Managed component catalog initialization failed. Apply the Stage 15 migration before starting the backend.");
            throw;
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var service = scope.ServiceProvider
            .GetRequiredService<ManagedComponentCatalogService>();
        await service.RefreshRuntimeAliasesAsync(cancellationToken);
    }
}
