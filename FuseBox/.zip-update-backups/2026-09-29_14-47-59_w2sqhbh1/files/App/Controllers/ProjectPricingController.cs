using System.Security.Claims;
using FuseBox.App.Contracts.Common;
using FuseBox.App.Services.Pricing;
using FuseBox.App.Services.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuseBox.App.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectPricingController : ControllerBase
{
    private readonly ProjectService _projects;
    private readonly PanelPricingService _pricing;
    private readonly ILogger<ProjectPricingController> _logger;

    public ProjectPricingController(
        ProjectService projects,
        PanelPricingService pricing,
        ILogger<ProjectPricingController> logger)
    {
        _projects = projects;
        _pricing = pricing;
        _logger = logger;
    }

    [HttpGet("{id:int}/pricing")]
    public async Task<IActionResult> GetPricing(
        int id,
        [FromQuery] bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return AuthenticationRequired();

        if (id <= 0)
            return ProjectNotFound();

        var schemaProject = await _projects.GetOwnedSchemaProjectAsync(
            userId,
            id,
            cancellationToken);

        if (schemaProject == null)
            return ProjectNotFound();

        var configurationProject =
            await _projects.GetOwnedConfigurationProjectAsync(
                userId,
                id,
                cancellationToken);

        if (configurationProject?.InitialSettings == null)
            return ProjectNotFound();

        try
        {
            var response = await _pricing.CalculateAsync(
                schemaProject,
                configurationProject.InitialSettings,
                refresh,
                cancellationToken);

            return Ok(response);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Pricing calculation failed for project {ProjectId} and user {UserId}.",
                id,
                userId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Error(
                    "pricing_failed",
                    "The panel specification could not be priced."));
        }
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId) && userId > 0;
    }

    private IActionResult AuthenticationRequired() =>
        Unauthorized(Error(
            ApiErrorCodes.AuthenticationRequired,
            "Authentication is required."));

    private IActionResult ProjectNotFound() =>
        NotFound(Error(
            ApiErrorCodes.ProjectNotFound,
            "Project not found."));

    private static ApiErrorResponse Error(
        string code,
        string message) =>
        new()
        {
            Code = code,
            Message = message
        };
}
