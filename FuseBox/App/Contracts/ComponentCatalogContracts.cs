using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts.Catalog;

public sealed class ComponentCatalogItemResponse
{
    public int Id { get; init; }
    public string CatalogTypeId { get; init; } = string.Empty;
    public string TemplateCatalogTypeId { get; init; } = string.Empty;
    public bool IsBuiltIn { get; init; }

    public string DisplayName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Manufacturer { get; init; }
    public string? Mpn { get; init; }
    public decimal? ReferencePriceEur { get; init; }
    public string? DatasheetUrl { get; init; }
    public string? ImageUrl { get; init; }
    public string? Model3dUrl { get; init; }

    public double ModelScale { get; init; }
    public double ModelRotationX { get; init; }
    public double ModelRotationY { get; init; }
    public double ModelRotationZ { get; init; }
    public double ModelOffsetX { get; init; }
    public double ModelOffsetY { get; init; }
    public double ModelOffsetZ { get; init; }

    public double? DefaultAmperage { get; init; }
    public int? DefaultRcdMilliAmps { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public bool Selectable { get; init; }

    // Read-only electrical template facts. Admin cannot rewrite terminals from
    // the CRUD page and accidentally corrupt saved schemas.
    public string Role { get; init; } = string.Empty;
    public string Mounting { get; init; } = string.Empty;
    public int Slots { get; init; }
    public string TerminalModel { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class ComponentCatalogPageResponse
{
    public IReadOnlyList<ComponentCatalogItemResponse> Items { get; init; } =
        Array.Empty<ComponentCatalogItemResponse>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class ComponentCatalogTemplateResponse
{
    public string CatalogTypeId { get; init; } = string.Empty;
    public string LegacyName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Mounting { get; init; } = string.Empty;
    public int Slots { get; init; }
    public string TerminalModel { get; init; } = string.Empty;
}

public sealed class CreateComponentCatalogItemRequest
{
    [Required, StringLength(96, MinimumLength = 3)]
    public string CatalogTypeId { get; set; } = string.Empty;

    [Required, StringLength(96)]
    public string TemplateCatalogTypeId { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string Category { get; set; } = "Other";

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(160)]
    public string? Manufacturer { get; set; }

    [StringLength(128)]
    public string? Mpn { get; set; }

    [Range(typeof(decimal), "0", "9999999999")]
    public decimal? ReferencePriceEur { get; set; }

    [StringLength(1024)]
    public string? DatasheetUrl { get; set; }

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    [StringLength(1024)]
    public string? Model3dUrl { get; set; }

    [Range(0.0001, 10000)]
    public double ModelScale { get; set; } = 1d;

    [Range(-3600, 3600)] public double ModelRotationX { get; set; }
    [Range(-3600, 3600)] public double ModelRotationY { get; set; }
    [Range(-3600, 3600)] public double ModelRotationZ { get; set; }
    [Range(-100, 100)] public double ModelOffsetX { get; set; }
    [Range(-100, 100)] public double ModelOffsetY { get; set; }
    [Range(-100, 100)] public double ModelOffsetZ { get; set; }

    [Range(0.01, 10000)]
    public double? DefaultAmperage { get; set; }

    [Range(1, 10000)]
    public int? DefaultRcdMilliAmps { get; set; }

    [Range(-100000, 100000)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class UpdateComponentCatalogItemRequest
{
    [Required, StringLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string Category { get; set; } = "Other";

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(160)]
    public string? Manufacturer { get; set; }

    [StringLength(128)]
    public string? Mpn { get; set; }

    [Range(typeof(decimal), "0", "9999999999")]
    public decimal? ReferencePriceEur { get; set; }

    [StringLength(1024)]
    public string? DatasheetUrl { get; set; }

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    [StringLength(1024)]
    public string? Model3dUrl { get; set; }

    [Range(0.0001, 10000)]
    public double ModelScale { get; set; } = 1d;

    [Range(-3600, 3600)] public double ModelRotationX { get; set; }
    [Range(-3600, 3600)] public double ModelRotationY { get; set; }
    [Range(-3600, 3600)] public double ModelRotationZ { get; set; }
    [Range(-100, 100)] public double ModelOffsetX { get; set; }
    [Range(-100, 100)] public double ModelOffsetY { get; set; }
    [Range(-100, 100)] public double ModelOffsetZ { get; set; }

    [Range(0.01, 10000)]
    public double? DefaultAmperage { get; set; }

    [Range(1, 10000)]
    public int? DefaultRcdMilliAmps { get; set; }

    [Range(-100000, 100000)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
