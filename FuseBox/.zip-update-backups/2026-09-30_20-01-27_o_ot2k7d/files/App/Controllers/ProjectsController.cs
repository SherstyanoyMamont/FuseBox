using System.Security.Claims;
using FuseBox.App.Contracts.Common;
using FuseBox.App.Contracts.Projects;
using FuseBox.App.Services.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuseBox.App.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/projects")]
    public sealed class ProjectsController : ControllerBase
    {
        private readonly ProjectService _projects;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            ProjectService projects,
            ILogger<ProjectsController> logger)
        {
            _projects = projects;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects(
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return AuthenticationRequired();

            var projects =
                await _projects.GetOwnedProjectsAsync(
                    userId,
                    cancellationToken);

            return Ok(projects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(
            [FromBody] ProjectSaveRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return AuthenticationRequired();

            if (!ModelState.IsValid)
                return ValidationFailed();

            try
            {
                var draft =
                    ProjectRequestMapper.ToProject(request);

                var created = await _projects.CreateAsync(
                    userId,
                    draft,
                    request.Name,
                    cancellationToken);

                return Created(
                    $"/api/projects/{created.Id}/configuration",
                    created);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(Error(
                    ApiErrorCodes.ValidationFailed,
                    exception.Message));
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Project creation failed for user {UserId}.",
                    userId);

                return Conflict(Error(
                    ApiErrorCodes.ProjectConflict,
                    "The project could not be created."));
            }
        }

        [HttpDelete("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return AuthenticationRequired();

            if (id <= 0)
                return ProjectNotFound();

            try
            {
                var deleted = await _projects.DeleteOwnedAsync(
                    userId,
                    id,
                    cancellationToken);

                return deleted
                    ? NoContent()
                    : ProjectNotFound();
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Project deletion failed for project {ProjectId} and user {UserId}.",
                    id,
                    userId);

                return Conflict(Error(
                    ApiErrorCodes.ProjectConflict,
                    "The project could not be deleted."));
            }
        }

        [HttpPut("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProject(
            int id,
            [FromBody] ProjectSaveRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return AuthenticationRequired();

            if (id <= 0)
                return ProjectNotFound();

            if (!ModelState.IsValid)
                return ValidationFailed();

            try
            {
                var draft =
                    ProjectRequestMapper.ToProject(request);

                var updated = await _projects.UpdateAsync(
                    userId,
                    id,
                    draft,
                    request.Name,
                    cancellationToken);

                return updated == null
                    ? ProjectNotFound()
                    : Ok(updated);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(Error(
                    ApiErrorCodes.ValidationFailed,
                    exception.Message));
            }
            catch (CustomizedSchemaConflictException exception)
            {
                return Conflict(Error(
                    ApiErrorCodes.SchemaCustomizationConflict,
                    exception.Message));
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Project update failed for project {ProjectId} " +
                    "and user {UserId}.",
                    id,
                    userId);

                return Conflict(Error(
                    ApiErrorCodes.ProjectConflict,
                    "The project could not be updated."));
            }
        }

        [HttpGet("{id:int}/schema")]
        public async Task<IActionResult> GetSchema(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return AuthenticationRequired();

            var project =
                await _projects.GetOwnedSchemaProjectAsync(
                    userId,
                    id,
                    cancellationToken);

            if (project == null)
                return ProjectNotFound();

            try
            {
                return Content(
                    ProjectResponseFactory
                        .BuildSchemaJson(project),
                    "application/json");
            }
            catch (global::FuseBox.App.SchemaEditor.SchemaCatalogException exception)
            {
                _logger.LogError(
                    exception,
                    "Schema catalog deployment failure for project {ProjectId}.",
                    id);

                return Problem(
                    title: "Schema catalog unavailable.",
                    detail: "The backend schema catalog could not be loaded. Rebuild and restart the backend.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(
                    exception,
                    "Invalid stored schema for project {ProjectId}.",
                    id);

                return Problem(
                    title: "Invalid project schema.",
                    detail:
                        "The stored schema is inconsistent.",
                    statusCode:
                        StatusCodes
                            .Status500InternalServerError);
            }
        }

        [HttpGet("{id:int}/configuration")]
        public async Task<IActionResult> GetConfiguration(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return AuthenticationRequired();

            var project =
                await _projects
                    .GetOwnedConfigurationProjectAsync(
                        userId,
                        id,
                        cancellationToken);

            if (project == null)
                return ProjectNotFound();

            if (ProjectResponseFactory
                .HasUnknownPower(project))
            {
                return Conflict(Error(
                    ApiErrorCodes.ProjectConflict,
                    "This legacy project does not contain " +
                    "consumer power in watts and cannot be " +
                    "edited until the values are restored."));
            }

            return Content(
                ProjectResponseFactory
                    .BuildConfigurationJson(project),
                "application/json");
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out userId) &&
                   userId > 0;
        }

        private IActionResult AuthenticationRequired()
        {
            return Unauthorized(Error(
                ApiErrorCodes.AuthenticationRequired,
                "Authentication is required."));
        }

        private IActionResult ProjectNotFound()
        {
            // Same response for missing and foreign projects.
            return NotFound(Error(
                ApiErrorCodes.ProjectNotFound,
                "Project not found."));
        }

        private IActionResult ValidationFailed()
        {
            var errors = ModelState
                .Where(entry =>
                    entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error =>
                            string.IsNullOrWhiteSpace(
                                error.ErrorMessage)
                                ? "Invalid value."
                                : error.ErrorMessage)
                        .ToArray());

            return BadRequest(new ApiErrorResponse
            {
                Code = ApiErrorCodes.ValidationFailed,
                Message = "Validation failed.",
                Errors = errors
            });
        }

        private static ApiErrorResponse Error(
            string code,
            string message)
        {
            return new ApiErrorResponse
            {
                Code = code,
                Message = message
            };
        }
    }
}


