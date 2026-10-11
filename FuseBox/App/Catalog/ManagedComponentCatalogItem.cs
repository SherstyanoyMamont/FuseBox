namespace FuseBox.App.Catalog;

public sealed class ManagedComponentCatalogItem
{
    public int Id { get; set; }

    // Stable product/catalog identity stored in customized schema documents.
    public string CatalogTypeId { get; set; } = string.Empty;

    // Electrical behavior is inherited from an immutable built-in template.
    // This keeps terminals/validation safe while allowing admins to add
    // commercial variants without shipping new backend code.
    public string TemplateCatalogTypeId { get; set; } = string.Empty;

    public bool IsBuiltIn { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? Mpn { get; set; }
    public decimal? ReferencePriceEur { get; set; }
    public string? DatasheetUrl { get; set; }

    // URL can be external HTTPS or a local /uploads/components/... path.
    public string? ImageUrl { get; set; }
    public string? Model3dUrl { get; set; }

    // Transform knobs make arbitrary GLB files usable without rebuilding UI.
    public double ModelScale { get; set; } = 1d;
    public double ModelRotationX { get; set; }
    public double ModelRotationY { get; set; }
    public double ModelRotationZ { get; set; }
    public double ModelOffsetX { get; set; }
    public double ModelOffsetY { get; set; }
    public double ModelOffsetZ { get; set; }

    // Optional defaults used when a newly added manual product is placed.
    public double? DefaultAmperage { get; set; }
    public int? DefaultRcdMilliAmps { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // DELETE is deliberately archival. Old projects keep resolving the
    // electrical definition and assets even after the item disappears from
    // the palette for new work.
    public bool IsDeleted { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
