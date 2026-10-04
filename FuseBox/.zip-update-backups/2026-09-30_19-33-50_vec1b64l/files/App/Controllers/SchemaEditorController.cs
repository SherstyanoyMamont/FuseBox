using System.Security.Claims;
using FuseBox.App.Contracts.Common;
using FuseBox.App.SchemaEditor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuseBox.App.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/schema")]
public sealed class SchemaEditorController : ControllerBase
{
    private readonly SchemaEditorService _editor;
    private readonly ILogger<SchemaEditorController> _logger;

    public SchemaEditorController(
        SchemaEditorService editor,
        ILogger<SchemaEditorController> logger)
    {
        _editor = editor;
        _logger = logger;
    }

    [HttpGet("editor")]
    public async Task<IActionResult> GetEditor(
        int projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return AuthenticationRequired();

        if (projectId <= 0)
            return ProjectNotFound();

        try
        {
            var document = await _editor.GetAsync(
                userId,
                projectId,
                cancellationToken);

            return document == null
                ? ProjectNotFound()
                : Ok(document);
        }
        catch (SchemaCatalogException exception)
        {
            _logger.LogError(
                exception,
                "Schema catalog deployment failure for project {ProjectId}.",
                projectId);

            return Problem(
                title: "Schema catalog unavailable.",
                detail:
                    "The backend schema catalog could not be loaded. " +
                    "Rebuild and restart the backend.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(
                exception,
                "Invalid editor state for project {ProjectId}.",
                projectId);

            return Problem(
                title: "Invalid schema editor state.",
                detail: "The stored schema editor state is inconsistent.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("editor/validate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateEditor(
        int projectId,
        [FromBody] SchemaDocument? schema,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return AuthenticationRequired();

        if (projectId <= 0)
            return ProjectNotFound();

        if (schema == null || !ModelState.IsValid)
            return ValidationFailed();

        try
        {
            var validation = await _editor.ValidateAsync(
                userId,
                projectId,
                schema,
                cancellationToken);

            return validation == null
                ? ProjectNotFound()
                : Ok(validation);
        }
        catch (SchemaCatalogException exception)
        {
            _logger.LogError(
                exception,
                "Schema catalog deployment failure while validating project {ProjectId}.",
                projectId);

            return Problem(
                title: "Schema catalog unavailable.",
                detail:
                    "The backend schema catalog could not be loaded. " +
                    "Rebuild and restart the backend.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("editor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEditor(
        int projectId,
        [FromBody] SaveSchemaEditorRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return AuthenticationRequired();

        if (projectId <= 0)
            return ProjectNotFound();

        var schema = request?.Schema;

        if (request == null || schema == null || !ModelState.IsValid)
            return ValidationFailed();

        try
        {
            var result = await _editor.SaveAsync(
                userId,
                projectId,
                request.ExpectedRevision,
                schema,
                cancellationToken);

            return ToWriteResult(result);
        }
        catch (SchemaCatalogException exception)
        {
            _logger.LogError(
                exception,
                "Schema catalog deployment failure while saving project {ProjectId}.",
                projectId);

            return Problem(
                title: "Schema catalog unavailable.",
                detail:
                    "The backend schema catalog could not be loaded. " +
                    "Rebuild and restart the backend.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("regenerate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Regenerate(
        int projectId,
        [FromBody] RegenerateSchemaRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return AuthenticationRequired();

        if (projectId <= 0)
            return ProjectNotFound();

        if (request == null || !ModelState.IsValid)
            return ValidationFailed();

        try
        {
            var result = await _editor.RegenerateAsync(
                userId,
                projectId,
                request.ExpectedRevision,
                cancellationToken);

            return ToWriteResult(result);
        }
        catch (SchemaCatalogException exception)
        {
            _logger.LogError(
                exception,
                "Schema catalog deployment failure while regenerating project {ProjectId}.",
                projectId);

            return Problem(
                title: "Schema catalog unavailable.",
                detail:
                    "The backend schema catalog could not be loaded. " +
                    "Rebuild and restart the backend.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception exception)
            when (exception is ArgumentException or InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "Schema regeneration failed for project {ProjectId}.",
                projectId);

            return Conflict(Error(
                ApiErrorCodes.ProjectConflict,
                "The generated schema could not be regenerated from the current project configuration."));
        }
    }

    private IActionResult ToWriteResult(SchemaEditorWriteResult result)
    {
        return result.Status switch
        {
            SchemaEditorWriteStatus.Success => Ok(result.Document),
            SchemaEditorWriteStatus.NotFound => ProjectNotFound(),
            SchemaEditorWriteStatus.RevisionConflict => Conflict(
                new ApiErrorResponse
                {
                    Code = ApiErrorCodes.SchemaRevisionConflict,
                    Message =
                        "The schema changed in another request. Reload the editor before saving.",
                    Errors = new Dictionary<string, string[]>
                    {
                        ["currentRevision"] = new[]
                        {
                            (result.CurrentRevision ?? 1).ToString()
                        }
                    }
                }),
            SchemaEditorWriteStatus.ValidationFailed => BadRequest(
                new SchemaSaveValidationErrorResponse
                {
                    Errors = result.Validation?.Errors ??
                        Array.Empty<SchemaValidationError>()
                }),
            _ => Problem(
                title: "Unexpected schema editor result.",
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId) && userId > 0;
    }

    private IActionResult AuthenticationRequired()
    {
        return Unauthorized(Error(
            ApiErrorCodes.AuthenticationRequired,
            "Authentication is required."));
    }

    private IActionResult ProjectNotFound()
    {
        return NotFound(Error(
            ApiErrorCodes.ProjectNotFound,
            "Project not found."));
    }

    private IActionResult ValidationFailed()
    {
        var errors = ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Invalid value."
                            : error.ErrorMessage)
                    .ToArray());

        if (errors.Count == 0)
        {
            errors["schema"] = new[] { "A schema document is required." };
        }

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
