using FuseBox.App.Contracts.Catalog;
using FuseBox.App.SchemaEditor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuseBox.App.Catalog;

[ApiController]
[Route("api/catalog/components")]
public sealed class ComponentCatalogController : ControllerBase
{
    private readonly ManagedComponentCatalogService _catalog;

    public ComponentCatalogController(ManagedComponentCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ComponentCatalogItemResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var items = await _catalog.GetPublicItemsAsync(cancellationToken);
        return Ok(items.Select(item =>
            ManagedComponentCatalogService.ToResponse(item, PublicUrl)).ToArray());
    }

    [HttpGet("templates")]
    [Authorize(Policy = "Admin")]
    public ActionResult<IReadOnlyList<ComponentCatalogTemplateResponse>> GetTemplates()
    {
        return Ok(ComponentCatalog.BuiltInEntries
            .Where(entry => entry.Mounting == "din" && entry.Slots > 0)
            .OrderBy(entry => entry.Role)
            .ThenBy(entry => entry.LegacyName)
            .Select(entry => new ComponentCatalogTemplateResponse
            {
                CatalogTypeId = entry.Id,
                LegacyName = entry.LegacyName,
                Role = entry.Role,
                Mounting = entry.Mounting,
                Slots = entry.Slots,
                TerminalModel = entry.TerminalModel
            })
            .ToArray());
    }

    private string? PublicUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Uri.TryCreate(value, UriKind.Absolute, out _))
            return value;

        var path = value.StartsWith('/') ? value : "/" + value;
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{path}";
    }
}
