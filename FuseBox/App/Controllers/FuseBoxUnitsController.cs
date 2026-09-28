using System.Security.Claims;
using AutoMapper;
using FuseBox.App.Contracts.Common;
using FuseBox.App.Models.DTO;
using FuseBox.App.Services.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FuseBox.Controllers
{
    public class FuseBoxUnitProfile : Profile
    {
        public FuseBoxUnitProfile()
        {
            CreateMap<FuseBoxUnitDTO, FuseBoxUnit>()
                .ForMember(
                    destination => destination.Project,
                    options => options.Ignore());

            CreateMap<FuseBoxUnit, FuseBoxUnitDTO>();
        }
    }

    /// <summary>
    /// Compatibility routes for the pre-stage-7 frontend.
    /// They are authenticated now and no longer assign a test/legacy owner.
    /// New frontend code should use /api/projects.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("")]
    public sealed class FuseBoxUnitsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ProjectService _projects;
        private readonly ILogger<FuseBoxUnitsController> _logger;

        public FuseBoxUnitsController(
            IMapper mapper,
            ProjectService projects,
            ILogger<FuseBoxUnitsController> logger)
        {
            _mapper = mapper;
            _projects = projects;
            _logger = logger;
        }

        [HttpPost("calculation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateFuseBox(
            [FromBody] ProjectDTO dto,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return AuthenticationRequired();

            if (dto == null)
            {
                return BadRequest(
                    "Project payload is missing.");
            }

            if (dto.FuseBox == null)
            {
                return BadRequest(
                    "FuseBox settings are missing.");
            }

            if (dto.Floors == null ||
                !dto.Floors.Any())
            {
                return BadRequest(
                    "At least one floor is required.");
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var draft = _mapper.Map<Project>(dto);

                var created = await _projects.CreateAsync(
                    userId,
                    draft,
                    dto.Name,
                    cancellationToken);

                // Keep the exact Pascal-case response expected by the
                // stage-3 ProjectContext.readProjectId().
                return Content(
                    JsonConvert.SerializeObject(
                        new
                        {
                            Id = created.Id
                        },
                        Formatting.Indented),
                    "application/json");
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Legacy calculation route failed for user {UserId}.",
                    userId);

                return Problem(
                    title: "Project save failed.",
                    detail:
                        "The project could not be saved.",
                    statusCode:
                        StatusCodes
                            .Status500InternalServerError);
            }
        }

        [HttpGet("project/{id:int}")]
        public async Task<IActionResult> GetProject(
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

        [HttpGet("project/{id:int}/configuration")]
        public async Task<IActionResult> GetProjectConfiguration(
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
                return Conflict(new
                {
                    message =
                        "This legacy project does not contain " +
                        "consumer power in watts and cannot be " +
                        "edited until the values are restored."
                });
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
            return Unauthorized(new ApiErrorResponse
            {
                Code =
                    ApiErrorCodes.AuthenticationRequired,
                Message =
                    "Authentication is required."
            });
        }

        private IActionResult ProjectNotFound()
        {
            return NotFound(new ApiErrorResponse
            {
                Code = ApiErrorCodes.ProjectNotFound,
                Message = "Project not found."
            });
        }
    }
}
