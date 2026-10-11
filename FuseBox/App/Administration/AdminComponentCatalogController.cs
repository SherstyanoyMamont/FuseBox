using System.Security.Claims;
using System.Text.RegularExpressions;
using FuseBox.App.Catalog;
using FuseBox.App.Contracts.Catalog;
using FuseBox.App.Contracts.Common;
using FuseBox.App.DataBase;
using FuseBox.App.SchemaEditor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Administration;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/components")]
public sealed class AdminComponentCatalogController : ControllerBase
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 100;
    private const long MaxImageBytes = 5L * 1024L * 1024L;
    private const long MaxModelBytes = 30L * 1024L * 1024L;

    private static readonly Regex CatalogTypeIdPattern = new(
        "^[a-z0-9][a-z0-9._-]{2,95}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<string, string> ImageExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp"
        };

    private readonly AppDbContext _context;
    private readonly ManagedComponentCatalogService _catalog;
    private readonly IWebHostEnvironment _environment;

    public AdminComponentCatalogController(
        AppDbContext context,
        ManagedComponentCatalogService catalog,
        IWebHostEnvironment environment)
    {
        _context = context;
        _catalog = catalog;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<ComponentCatalogPageResponse>> Get(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.ManagedComponentCatalogItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(item =>
                item.DisplayName.Contains(value) ||
                item.CatalogTypeId.Contains(value) ||
                (item.Manufacturer != null && item.Manufacturer.Contains(value)) ||
                (item.Mpn != null && item.Mpn.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var value = category.Trim();
            query = query.Where(item => item.Category == value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status.Trim().ToLowerInvariant())
            {
                case "active":
                    query = query.Where(item => item.IsActive && !item.IsDeleted);
                    break;
                case "inactive":
                    query = query.Where(item => !item.IsActive && !item.IsDeleted);
                    break;
                case "archived":
                case "deleted":
                    query = query.Where(item => item.IsDeleted);
                    break;
                case "all":
                    break;
                default:
                    return BadRequest(Error(
                        "admin.catalog.invalid_status",
                        "Status must be active, inactive, archived or all."));
            }
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.DisplayName)
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return Ok(new ComponentCatalogPageResponse
        {
            Items = items.Select(ToResponse).ToArray(),
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ComponentCatalogItemResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await _context.ManagedComponentCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return item == null
            ? NotFound(Error("admin.catalog.not_found", "Component catalog item not found."))
            : Ok(ToResponse(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ComponentCatalogItemResponse>> Create(
        [FromBody] CreateComponentCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationError());

        var catalogTypeId = NormalizeCatalogTypeId(request.CatalogTypeId);
        if (!CatalogTypeIdPattern.IsMatch(catalogTypeId))
        {
            return BadRequest(Error(
                "admin.catalog.invalid_catalog_type_id",
                "catalogTypeId may contain lowercase letters, digits, dots, underscores and hyphens."));
        }

        if (catalogTypeId.StartsWith("fusebox.", StringComparison.Ordinal))
        {
            return BadRequest(Error(
                "admin.catalog.reserved_catalog_type_id",
                "The fusebox.* catalogTypeId namespace is reserved for built-in electrical definitions."));
        }

        if (ComponentCatalog.ResolveBuiltInById(catalogTypeId) != null ||
            await _context.ManagedComponentCatalogItems.AnyAsync(
                item => item.CatalogTypeId == catalogTypeId,
                cancellationToken))
        {
            return Conflict(Error(
                "admin.catalog.catalog_type_exists",
                "A component with this catalogTypeId already exists."));
        }

        var templateId = request.TemplateCatalogTypeId.Trim();
        var template = ComponentCatalog.ResolveBuiltInById(templateId);
        if (template == null || template.Mounting != "din" || template.Slots <= 0)
        {
            return BadRequest(Error(
                "admin.catalog.invalid_template",
                "New components must inherit a valid built-in DIN component template."));
        }

        var defaultError = ValidateTemplateDefaults(
            template,
            request.DefaultAmperage,
            request.DefaultRcdMilliAmps);
        if (defaultError != null)
            return BadRequest(defaultError);

        if (!TryNormalizeUrls(
                request.DatasheetUrl,
                request.ImageUrl,
                request.Model3dUrl,
                out var datasheetUrl,
                out var imageUrl,
                out var model3dUrl,
                out var urlError))
        {
            return BadRequest(urlError!);
        }

        var now = DateTime.UtcNow;
        var item = new ManagedComponentCatalogItem
        {
            CatalogTypeId = catalogTypeId,
            TemplateCatalogTypeId = templateId,
            IsBuiltIn = false,
            DisplayName = request.DisplayName.Trim(),
            Category = request.Category.Trim(),
            Description = NormalizeText(request.Description),
            Manufacturer = NormalizeText(request.Manufacturer),
            Mpn = NormalizeText(request.Mpn),
            ReferencePriceEur = request.ReferencePriceEur,
            DatasheetUrl = datasheetUrl,
            ImageUrl = imageUrl,
            Model3dUrl = model3dUrl,
            ModelScale = request.ModelScale,
            ModelRotationX = request.ModelRotationX,
            ModelRotationY = request.ModelRotationY,
            ModelRotationZ = request.ModelRotationZ,
            ModelOffsetX = request.ModelOffsetX,
            ModelOffsetY = request.ModelOffsetY,
            ModelOffsetZ = request.ModelOffsetZ,
            DefaultAmperage = request.DefaultAmperage,
            DefaultRcdMilliAmps = request.DefaultRcdMilliAmps,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        _context.ManagedComponentCatalogItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        AddAudit(
            AdminAuditActions.CreateComponent,
            $"component #{item.Id} {item.CatalogTypeId}; template={item.TemplateCatalogTypeId}; name={item.DisplayName}");
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _catalog.RefreshRuntimeAliasesAsync(cancellationToken);
        return Created($"/api/admin/components/{item.Id}", ToResponse(item));
    }

    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ComponentCatalogItemResponse>> Update(
        int id,
        [FromBody] UpdateComponentCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationError());

        var item = await _context.ManagedComponentCatalogItems
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item == null)
            return NotFound(Error("admin.catalog.not_found", "Component catalog item not found."));

        var template = ComponentCatalog.ResolveBuiltInById(item.TemplateCatalogTypeId);
        if (template == null)
        {
            return Conflict(Error(
                "admin.catalog.template_missing",
                "The component's immutable electrical template is no longer available."));
        }

        var defaultError = ValidateTemplateDefaults(
            template,
            request.DefaultAmperage,
            request.DefaultRcdMilliAmps);
        if (defaultError != null)
            return BadRequest(defaultError);

        if (!TryNormalizeUrls(
                request.DatasheetUrl,
                request.ImageUrl,
                request.Model3dUrl,
                out var datasheetUrl,
                out var imageUrl,
                out var model3dUrl,
                out var urlError))
        {
            return BadRequest(urlError!);
        }

        var before = $"name={item.DisplayName}; active={item.IsActive}; price={item.ReferencePriceEur?.ToString() ?? "null"}; mpn={item.Mpn ?? "null"}";

        item.DisplayName = request.DisplayName.Trim();
        item.Category = request.Category.Trim();
        item.Description = NormalizeText(request.Description);
        item.Manufacturer = NormalizeText(request.Manufacturer);
        item.Mpn = NormalizeText(request.Mpn);
        item.ReferencePriceEur = request.ReferencePriceEur;
        item.DatasheetUrl = datasheetUrl;
        item.ImageUrl = imageUrl;
        item.Model3dUrl = model3dUrl;
        item.ModelScale = request.ModelScale;
        item.ModelRotationX = request.ModelRotationX;
        item.ModelRotationY = request.ModelRotationY;
        item.ModelRotationZ = request.ModelRotationZ;
        item.ModelOffsetX = request.ModelOffsetX;
        item.ModelOffsetY = request.ModelOffsetY;
        item.ModelOffsetZ = request.ModelOffsetZ;
        item.DefaultAmperage = request.DefaultAmperage;
        item.DefaultRcdMilliAmps = request.DefaultRcdMilliAmps;
        item.SortOrder = request.SortOrder;
        item.IsActive = request.IsActive && !item.IsDeleted;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        AddAudit(
            AdminAuditActions.UpdateComponent,
            $"component #{item.Id} {item.CatalogTypeId}; {before} -> name={item.DisplayName}; active={item.IsActive}; price={item.ReferencePriceEur?.ToString() ?? "null"}; mpn={item.Mpn ?? "null"}");
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _catalog.RefreshRuntimeAliasesAsync(cancellationToken);
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ComponentCatalogItemResponse>> Archive(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await _context.ManagedComponentCatalogItems
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item == null)
            return NotFound(Error("admin.catalog.not_found", "Component catalog item not found."));

        if (!item.IsDeleted)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);
            item.IsDeleted = true;
            item.IsActive = false;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(
                AdminAuditActions.ArchiveComponent,
                $"component #{item.Id} {item.CatalogTypeId}; archived (soft delete)");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Keep alias resolvable for historical projects; public payload carries
        // isDeleted so it is hidden from new-placement palette only.
        await _catalog.RefreshRuntimeAliasesAsync(cancellationToken);
        return Ok(ToResponse(item));
    }

    [HttpPost("{id:int}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ComponentCatalogItemResponse>> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await _context.ManagedComponentCatalogItems
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item == null)
            return NotFound(Error("admin.catalog.not_found", "Component catalog item not found."));

        if (item.IsDeleted)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);
            item.IsDeleted = false;
            item.IsActive = true;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(
                AdminAuditActions.RestoreComponent,
                $"component #{item.Id} {item.CatalogTypeId}; restored");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await _catalog.RefreshRuntimeAliasesAsync(cancellationToken);
        return Ok(ToResponse(item));
    }

    [HttpPost("{id:int}/image")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImageBytes + 1024 * 1024)]
    public Task<ActionResult<ComponentCatalogItemResponse>> UploadImage(
        int id,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken) =>
        UploadAssetAsync(id, file, "image", MaxImageBytes, cancellationToken);

    [HttpPost("{id:int}/model")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxModelBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxModelBytes + 1024 * 1024)]
    public Task<ActionResult<ComponentCatalogItemResponse>> UploadModel(
        int id,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken) =>
        UploadAssetAsync(id, file, "model", MaxModelBytes, cancellationToken);

    [HttpDelete("{id:int}/image")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<ComponentCatalogItemResponse>> DeleteImage(
        int id,
        CancellationToken cancellationToken) =>
        DeleteAssetAsync(id, "image", cancellationToken);

    [HttpDelete("{id:int}/model")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<ComponentCatalogItemResponse>> DeleteModel(
        int id,
        CancellationToken cancellationToken) =>
        DeleteAssetAsync(id, "model", cancellationToken);

    private async Task<ActionResult<ComponentCatalogItemResponse>> UploadAssetAsync(
        int id,
        IFormFile file,
        string kind,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var item = await _context.ManagedComponentCatalogItems
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item == null)
            return NotFound(Error("admin.catalog.not_found", "Component catalog item not found."));

        if (file == null || file.Length <= 0 || file.Length > maxBytes)
        {
            return BadRequest(Error(
                "admin.catalog.invalid_asset",
                $"{kind} file is empty or exceeds the size limit."));
        }

        string extension;
        if (kind == "image")
        {
            extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ImageExtensions.TryGetValue(extension, out var expectedType) ||
                !string.Equals(file.ContentType, expectedType, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(Error(
                    "admin.catalog.invalid_image",
                    "Images must be PNG, JPEG or WebP."));
            }
        }
        else
        {
            extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".glb")
            {
                return BadRequest(Error(
                    "admin.catalog.invalid_model",
                    "3D models must be binary glTF (.glb) files."));
            }
        }

        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var directory = Path.Combine(webRoot, "uploads", "components", item.Id.ToString());
        Directory.CreateDirectory(directory);

        var version = Guid.NewGuid().ToString("N");
        var fileName = kind == "image"
            ? $"image-{version}{extension}"
            : $"model-{version}.glb";
        var physicalPath = Path.Combine(directory, fileName);
        var tempPath = physicalPath + ".tmp";

        await using (var stream = System.IO.File.Create(tempPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        if (kind == "image" && !IsValidImage(tempPath, extension))
        {
            System.IO.File.Delete(tempPath);
            return BadRequest(Error(
                "admin.catalog.invalid_image",
                "The uploaded file does not contain a valid PNG, JPEG or WebP image."));
        }

        if (kind == "model" && !IsValidGlb(tempPath))
        {
            System.IO.File.Delete(tempPath);
            return BadRequest(Error(
                "admin.catalog.invalid_model",
                "The uploaded file is not a valid binary glTF 2.x (.glb) container."));
        }

        System.IO.File.Move(tempPath, physicalPath);
        var storedUrl = $"/uploads/components/{item.Id}/{fileName}";
        var previousStoredUrl = kind == "image" ? item.ImageUrl : item.Model3dUrl;

        if (kind == "image")
            item.ImageUrl = storedUrl;
        else
            item.Model3dUrl = storedUrl;

        item.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(
                kind == "image"
                    ? AdminAuditActions.UploadComponentImage
                    : AdminAuditActions.UploadComponentModel,
                $"component #{item.Id} {item.CatalogTypeId}; {kind} asset replaced; bytes={file.Length}");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);
            }
            catch
            {
                // Preserve the original database exception. A deployment
                // storage cleanup can remove a rare orphan later.
            }

            throw;
        }

        TryDeleteManagedAsset(previousStoredUrl);
        return Ok(ToResponse(item));
    }

    private async Task<ActionResult<ComponentCatalogItemResponse>> DeleteAssetAsync(
        int id,
        string kind,
        CancellationToken cancellationToken)
    {
        var item = await _context.ManagedComponentCatalogItems
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item == null)
            return NotFound(Error("admin.catalog.not_found", "Component catalog item not found."));

        var storedUrl = kind == "image" ? item.ImageUrl : item.Model3dUrl;
        if (kind == "image")
            item.ImageUrl = null;
        else
            item.Model3dUrl = null;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        AddAudit(
            kind == "image"
                ? AdminAuditActions.DeleteComponentImage
                : AdminAuditActions.DeleteComponentModel,
            $"component #{item.Id} {item.CatalogTypeId}; {kind} asset removed");
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TryDeleteManagedAsset(storedUrl);
        return Ok(ToResponse(item));
    }

    private static bool IsValidImage(string path, string extension)
    {
        try
        {
            using var stream = System.IO.File.OpenRead(path);
            Span<byte> header = stackalloc byte[12];
            var read = stream.Read(header);

            return extension switch
            {
                ".png" => read >= 8 &&
                          header[0] == 0x89 &&
                          header[1] == 0x50 &&
                          header[2] == 0x4E &&
                          header[3] == 0x47 &&
                          header[4] == 0x0D &&
                          header[5] == 0x0A &&
                          header[6] == 0x1A &&
                          header[7] == 0x0A,
                ".jpg" or ".jpeg" => read >= 3 &&
                                      header[0] == 0xFF &&
                                      header[1] == 0xD8 &&
                                      header[2] == 0xFF,
                ".webp" => read >= 12 &&
                            header[0] == (byte)'R' &&
                            header[1] == (byte)'I' &&
                            header[2] == (byte)'F' &&
                            header[3] == (byte)'F' &&
                            header[8] == (byte)'W' &&
                            header[9] == (byte)'E' &&
                            header[10] == (byte)'B' &&
                            header[11] == (byte)'P',
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidGlb(string path)
    {
        try
        {
            using var stream = System.IO.File.OpenRead(path);
            if (stream.Length < 12) return false;

            Span<byte> header = stackalloc byte[12];
            if (stream.Read(header) != header.Length) return false;

            var magic = BitConverter.ToUInt32(header[..4]);
            var version = BitConverter.ToUInt32(header.Slice(4, 4));
            var declaredLength = BitConverter.ToUInt32(header.Slice(8, 4));

            // ASCII "glTF" little-endian, glTF 2.x container.
            return magic == 0x46546C67 &&
                   version == 2 &&
                   declaredLength == stream.Length;
        }
        catch
        {
            return false;
        }
    }

    private void TryDeleteManagedAsset(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl) ||
            !storedUrl.StartsWith("/uploads/components/", StringComparison.Ordinal))
            return;

        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var relative = storedUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(webRoot, relative));
        var expectedRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "components"));

        if (!full.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }
        catch
        {
            // Asset removal is intentionally best-effort after the DB state is
            // safely updated. Orphans can be cleaned by deployment storage jobs.
        }
    }

    private ComponentCatalogItemResponse ToResponse(ManagedComponentCatalogItem item) =>
        ManagedComponentCatalogService.ToResponse(item, PublicUrl);

    private string? PublicUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (Uri.TryCreate(value, UriKind.Absolute, out _))
            return value;
        var path = value.StartsWith('/') ? value : "/" + value;
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{path}";
    }

    private void AddAudit(string action, string details)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            throw new InvalidOperationException("Admin identity is missing a user ID.");

        var email = User.FindFirstValue(ClaimTypes.Email) ??
                    User.Identity?.Name ??
                    $"user:{actorId}";

        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorUserId = actorId,
            ActorEmail = email,
            Action = action,
            TargetUserId = null,
            TargetEmail = null,
            Details = details.Length <= 1024 ? details : details[..1024],
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static ApiErrorResponse? ValidateTemplateDefaults(
        CatalogEntry template,
        double? defaultAmperage,
        int? defaultRcdMilliAmps)
    {
        if (template.Role == "mcb" &&
            defaultAmperage.HasValue &&
            (defaultAmperage.Value <= 0 || defaultAmperage.Value > 125))
        {
            return Error(
                "admin.catalog.invalid_default_amperage",
                "MCB default amperage must be greater than 0 A and at most 125 A.");
        }

        if (template.Role == "rcd" &&
            defaultRcdMilliAmps.HasValue &&
            defaultRcdMilliAmps.Value is not (10 or 30 or 100 or 300 or 500))
        {
            return Error(
                "admin.catalog.invalid_default_rcd",
                "RCD default sensitivity must be 10, 30, 100, 300 or 500 mA.");
        }

        return null;
    }

    private static string NormalizeCatalogTypeId(string value) =>
        value.Trim().ToLowerInvariant();

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool TryNormalizeUrls(
        string? datasheet,
        string? image,
        string? model,
        out string? normalizedDatasheet,
        out string? normalizedImage,
        out string? normalizedModel,
        out ApiErrorResponse? error)
    {
        normalizedDatasheet = NormalizeUrl(datasheet);
        normalizedImage = NormalizeUrl(image);
        normalizedModel = NormalizeUrl(model);
        error = null;

        if (datasheet != null && normalizedDatasheet == null && !string.IsNullOrWhiteSpace(datasheet) ||
            image != null && normalizedImage == null && !string.IsNullOrWhiteSpace(image) ||
            model != null && normalizedModel == null && !string.IsNullOrWhiteSpace(model))
        {
            error = Error(
                "admin.catalog.invalid_url",
                "Asset and datasheet URLs must be HTTPS URLs or local /uploads/... paths. HTTP is accepted only in Development.");
            return false;
        }

        return true;
    }

    private string? NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.StartsWith("/uploads/", StringComparison.Ordinal))
            return trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http"))
            return null;

        if (uri.Scheme == "http" && !_environment.IsDevelopment())
            return null;

        // Admin responses expose local assets as absolute URLs so the separate
        // frontend origin can preview them. Convert our own host back to a
        // portable relative path before persisting it.
        if (string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
            (Request.Host.Port == null || uri.Port == Request.Host.Port) &&
            uri.AbsolutePath.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            return uri.PathAndQuery;
        }

        return uri.ToString();
    }

    private static ApiErrorResponse ValidationError() => new()
    {
        Code = ApiErrorCodes.ValidationFailed,
        Message = "Validation failed."
    };

    private static ApiErrorResponse Error(string code, string message) => new()
    {
        Code = code,
        Message = message
    };
}
