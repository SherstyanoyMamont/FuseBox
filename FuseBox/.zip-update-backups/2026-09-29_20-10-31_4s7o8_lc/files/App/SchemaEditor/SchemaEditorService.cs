using FuseBox.App.DataBase;
using FuseBox.App.Models;
using FuseBox.App.Services.Projects;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FuseBox.App.SchemaEditor;

public sealed class SchemaEditorService
{
    private readonly AppDbContext _context;
    private readonly ProjectService _projects;
    private readonly SchemaEditorValidator _validator;

    public SchemaEditorService(
        AppDbContext context,
        ProjectService projects,
        SchemaEditorValidator validator)
    {
        _context = context;
        _projects = projects;
        _validator = validator;
    }

    public async Task<SchemaDocument?> GetAsync(
        int userId,
        int projectId,
        CancellationToken cancellationToken)
    {
        var state = await _context.Projects
            .AsNoTracking()
            .Where(project =>
                project.Id == projectId &&
                project.UserId == userId)
            .Select(project => new
            {
                Mode = project.SchemaEditorState == null
                    ? SchemaModes.Generated
                    : project.SchemaEditorState.Mode,
                Revision = project.SchemaEditorState == null
                    ? 1
                    : project.SchemaEditorState.Revision,
                DocumentJson = project.SchemaEditorState == null
                    ? null
                    : project.SchemaEditorState.DocumentJson
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (state == null)
            return null;

        if (string.Equals(
                state.Mode,
                SchemaModes.Customized,
                StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(state.DocumentJson))
            {
                throw new InvalidOperationException(
                    "Customized schema metadata does not contain a document.");
            }

            var customized = JsonConvert.DeserializeObject<SchemaDocument>(
                state.DocumentJson)
                ?? throw new InvalidOperationException(
                    "Customized schema document cannot be deserialized.");

            NormalizeDocument(customized);
            customized.Mode = SchemaModes.Customized;
            customized.Revision = state.Revision;
            return customized;
        }

        if (!string.Equals(
                state.Mode,
                SchemaModes.Generated,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Stored schema mode is invalid.");
        }

        var project = await _projects.GetOwnedSchemaProjectAsync(
            userId,
            projectId,
            cancellationToken);

        return project == null
            ? null
            : GeneratedSchemaDocumentFactory.Create(project, state.Revision);
    }

    public async Task<SchemaValidationResponse?> ValidateAsync(
        int userId,
        int projectId,
        SchemaDocument document,
        CancellationToken cancellationToken)
    {
        NormalizeDocument(document);

        var validationContext = await GetValidationContextAsync(
            userId,
            projectId,
            cancellationToken);

        return validationContext == null
            ? null
            : _validator.Validate(document, validationContext);
    }

    public async Task<SchemaEditorWriteResult> SaveAsync(
        int userId,
        int projectId,
        int expectedRevision,
        SchemaDocument document,
        CancellationToken cancellationToken)
    {
        NormalizeDocument(document);

        var project = await _context.Projects
            .Where(candidate =>
                candidate.Id == projectId &&
                candidate.UserId == userId)
            .Include(candidate => candidate.SchemaEditorState)
            .Include(candidate => candidate.InitialSettings)
            .Include(candidate => candidate.FuseBox)
                .ThenInclude(fuseBox => fuseBox.ComponentGroups)
            .SingleOrDefaultAsync(cancellationToken);

        if (project == null)
        {
            return new SchemaEditorWriteResult
            {
                Status = SchemaEditorWriteStatus.NotFound
            };
        }

        var currentRevision = project.SchemaEditorState?.Revision ?? 1;

        if (expectedRevision != currentRevision)
        {
            return RevisionConflict(currentRevision);
        }

        var validation = _validator.Validate(
            document,
            new SchemaValidationContext
            {
                ShieldWidth = project.InitialSettings.ShieldWidth,
                RailCount = project.FuseBox.ComponentGroups.Count
            });

        if (!validation.Valid)
        {
            return new SchemaEditorWriteResult
            {
                Status = SchemaEditorWriteStatus.ValidationFailed,
                Validation = validation,
                CurrentRevision = currentRevision
            };
        }

        var nextRevision = checked(currentRevision + 1);
        document.Mode = SchemaModes.Customized;
        document.Revision = nextRevision;

        var json = JsonConvert.SerializeObject(
            document,
            Formatting.None);

        var now = DateTime.UtcNow;

        if (project.SchemaEditorState == null)
        {
            project.SchemaEditorState = new SchemaEditorState
            {
                Project = project,
                Mode = SchemaModes.Customized,
                Revision = nextRevision,
                DocumentJson = json,
                UpdatedAtUtc = now
            };
        }
        else
        {
            project.SchemaEditorState.Mode = SchemaModes.Customized;
            project.SchemaEditorState.Revision = nextRevision;
            project.SchemaEditorState.DocumentJson = json;
            project.SchemaEditorState.UpdatedAtUtc = now;
        }

        project.UpdatedAtUtc = now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return RevisionConflict(
                await GetCurrentRevisionAsync(projectId, cancellationToken));
        }
        catch (DbUpdateException)
        {
            // A legacy project may have no editor row. If another request
            // created the one-to-one row concurrently, the unique PK/FK insert
            // loses the race and must surface as a revision conflict.
            var latestRevision = await GetCurrentRevisionAsync(
                projectId,
                cancellationToken);

            if (latestRevision != currentRevision)
                return RevisionConflict(latestRevision);

            throw;
        }

        return new SchemaEditorWriteResult
        {
            Status = SchemaEditorWriteStatus.Success,
            Document = document,
            CurrentRevision = nextRevision
        };
    }

    public async Task<SchemaEditorWriteResult> RegenerateAsync(
        int userId,
        int projectId,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        var metadata = await _context.Projects
            .AsNoTracking()
            .Where(project =>
                project.Id == projectId &&
                project.UserId == userId)
            .Select(project => new
            {
                FuseBoxId = project.FuseBox.Id
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (metadata == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new SchemaEditorWriteResult
            {
                Status = SchemaEditorWriteStatus.NotFound
            };
        }

        var state = await _context.SchemaEditorStates
            .SingleOrDefaultAsync(
                candidate => candidate.ProjectId == projectId,
                cancellationToken);

        var currentRevision = state?.Revision ?? 1;

        if (expectedRevision != currentRevision)
        {
            await transaction.RollbackAsync(cancellationToken);
            return RevisionConflict(currentRevision);
        }

        await _projects.ClearGeneratedSchemaGraphAsync(
            projectId,
            metadata.FuseBoxId,
            cancellationToken);

        var project = await _projects.GetOwnedRegenerationProjectAsync(
            userId,
            projectId,
            cancellationToken);

        if (project == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new SchemaEditorWriteResult
            {
                Status = SchemaEditorWriteStatus.NotFound
            };
        }

        project.FuseBox.ComponentGroups = new List<global::FuseBox.FuseBoxComponentGroup>();
        project.FuseBox.CableConnections = new List<CableConnection>();

        ProjectService.GenerateConfiguration(project);
        ProjectService.BindGeneratedGraph(project);

        var nextRevision = checked(currentRevision + 1);
        var now = DateTime.UtcNow;

        if (state == null)
        {
            state = new SchemaEditorState
            {
                ProjectId = projectId,
                Project = project,
                Mode = SchemaModes.Generated,
                Revision = nextRevision,
                DocumentJson = null,
                UpdatedAtUtc = now
            };

            project.SchemaEditorState = state;
            _context.SchemaEditorStates.Add(state);
        }
        else
        {
            state.Mode = SchemaModes.Generated;
            state.Revision = nextRevision;
            state.DocumentJson = null;
            state.UpdatedAtUtc = now;
        }

        project.UpdatedAtUtc = now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return RevisionConflict(
                await GetCurrentRevisionAsync(projectId, cancellationToken));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);

            var latestRevision = await GetCurrentRevisionAsync(
                projectId,
                cancellationToken);

            if (latestRevision != currentRevision)
                return RevisionConflict(latestRevision);

            throw;
        }

        var generated = GeneratedSchemaDocumentFactory.Create(
            project,
            nextRevision);

        return new SchemaEditorWriteResult
        {
            Status = SchemaEditorWriteStatus.Success,
            Document = generated,
            CurrentRevision = nextRevision
        };
    }

    private async Task<SchemaValidationContext?> GetValidationContextAsync(
        int userId,
        int projectId,
        CancellationToken cancellationToken)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(project =>
                project.Id == projectId &&
                project.UserId == userId)
            .Select(project => new SchemaValidationContext
            {
                ShieldWidth = project.InitialSettings.ShieldWidth,
                RailCount = project.FuseBox.ComponentGroups.Count()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<int> GetCurrentRevisionAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var revision = await _context.SchemaEditorStates
            .AsNoTracking()
            .Where(state => state.ProjectId == projectId)
            .Select(state => (int?)state.Revision)
            .SingleOrDefaultAsync(cancellationToken);

        return revision ?? 1;
    }

    private static SchemaEditorWriteResult RevisionConflict(
        int currentRevision)
    {
        return new SchemaEditorWriteResult
        {
            Status = SchemaEditorWriteStatus.RevisionConflict,
            CurrentRevision = currentRevision
        };
    }

    private static void NormalizeDocument(SchemaDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Components ??= new List<EditorComponent>();
        document.Connections ??= new List<EditorConnection>();
        document.ReservedSlots ??= new List<DinPlacement>();
        document.UnresolvedConnections ??= new List<UnresolvedLegacyConnection>();
        document.ImportIssues ??= new List<SchemaImportIssue>();

        foreach (var component in document.Components)
            component.LinkedConsumerIds ??= new List<int>();

        foreach (var connection in document.Connections)
        {
            connection.From ??= new EditorEndpoint();
            connection.To ??= new EditorEndpoint();
        }
    }
}
