using FuseBox.App.Contracts.Projects;
using FuseBox.App.DataBase;
using FuseBox.App.Factorys;
using FuseBox.App.Interfaces;
using FuseBox.App.Models;
using FuseBox.App.Services.Providers;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Services.Projects
{
    public sealed class ProjectService
    {
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProjectSummaryResponse>>
            GetOwnedProjectsAsync(
                int userId,
                CancellationToken cancellationToken)
        {
            return await _context.Projects
                .AsNoTracking()
                .Where(project =>
                    project.UserId == userId)
                .OrderByDescending(project =>
                    project.UpdatedAtUtc)
                .ThenByDescending(project =>
                    project.Id)
                .Select(project =>
                    new ProjectSummaryResponse
                    {
                        Id = project.Id,
                        Name = project.Name ?? string.Empty,
                        CreatedAtUtc =
                            project.CreatedAtUtc,
                        UpdatedAtUtc =
                            project.UpdatedAtUtc
                    })
                .ToListAsync(cancellationToken);
        }

        public async Task<ProjectSummaryResponse> CreateAsync(
            int userId,
            Project draft,
            string? requestedName,
            CancellationToken cancellationToken)
        {
            var ownerExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(
                    user =>
                        user.Id == userId &&
                        user.PasswordHash != null,
                    cancellationToken);

            if (!ownerExists)
            {
                throw new InvalidOperationException(
                    "Authenticated account no longer exists.");
            }

            var now = DateTime.UtcNow;

            PrepareDraft(
                draft,
                userId,
                requestedName,
                createdAtUtc: now,
                updatedAtUtc: now);

            _context.Projects.Add(draft);
            await _context.SaveChangesAsync(
                cancellationToken);

            return ToSummary(draft);
        }

        public async Task<ProjectSummaryResponse?> UpdateAsync(
            int userId,
            int projectId,
            Project draft,
            string? requestedName,
            CancellationToken cancellationToken)
        {
            var metadata = await _context.Projects
                .AsNoTracking()
                .Where(project =>
                    project.Id == projectId &&
                    project.UserId == userId)
                .Select(project => new
                {
                    project.CreatedAtUtc
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (metadata == null)
                return null;

            var now = DateTime.UtcNow;

            PrepareDraft(
                draft,
                userId,
                requestedName,
                createdAtUtc: metadata.CreatedAtUtc,
                updatedAtUtc: now);

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            var existing = await _context.Projects
                .SingleOrDefaultAsync(
                    project =>
                        project.Id == projectId &&
                        project.UserId == userId,
                    cancellationToken);

            if (existing == null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return null;
            }

            await DeleteExistingGraphAsync(
                projectId,
                cancellationToken);

            existing.Name = draft.Name;
            existing.TotalPower = draft.TotalPower;
            existing.UpdatedAtUtc = now;

            AttachReplacementGraph(
                existing,
                draft);

            await _context.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return ToSummary(existing);
        }

        public async Task<Project?> GetOwnedSchemaProjectAsync(
            int userId,
            int projectId,
            CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Where(candidate =>
                    candidate.Id == projectId &&
                    candidate.UserId == userId)
                .Include(candidate =>
                    candidate.FuseBox.ComponentGroups)
                    .ThenInclude(group =>
                        group.Components)
                .Include(candidate =>
                    candidate.FuseBox.CableConnections)
                    .ThenInclude(connection =>
                        connection.Cable)
                .Include(candidate =>
                    candidate.FuseBox.CableConnections)
                    .ThenInclude(connection =>
                        connection.CabelWay)
                .FirstOrDefaultAsync(
                    cancellationToken);

            if (project?.FuseBox == null)
                return null;

            var devices = project.FuseBox
                .ComponentGroups
                .SelectMany(group =>
                    group.Components)
                .Where(component =>
                    component is not EmptySlot)
                .ToList();

            foreach (var fuse in devices.OfType<Fuse>())
            {
                await _context.Entry(fuse)
                    .Collection(component =>
                        component.Electricals)
                    .LoadAsync(cancellationToken);
            }

            return project;
        }

        public async Task<Project?>
            GetOwnedConfigurationProjectAsync(
                int userId,
                int projectId,
                CancellationToken cancellationToken)
        {
            return await _context.Projects
                .AsNoTracking()
                .Where(project =>
                    project.Id == projectId &&
                    project.UserId == userId)
                .Include(project =>
                    project.InitialSettings)
                .Include(project =>
                    project.FuseBox)
                .Include(project =>
                    project.Floors)
                    .ThenInclude(floor =>
                        floor.Rooms)
                        .ThenInclude(room =>
                            room.Consumer)
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        private static void PrepareDraft(
            Project project,
            int userId,
            string? requestedName,
            DateTime createdAtUtc,
            DateTime updatedAtUtc)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (project.Floors == null ||
                project.Floors.Count == 0)
            {
                throw new ArgumentException(
                    "At least one floor is required.");
            }

            project.Id = 0;
            project.UserId = userId;
            project.Name = NormalizeProjectName(
                requestedName,
                updatedAtUtc);
            project.CreatedAtUtc = createdAtUtc;
            project.UpdatedAtUtc = updatedAtUtc;

            SanitizeInputGraph(project);
            BindInputGraph(project);
            PrepareConsumerLoads(project);
            GenerateConfiguration(project);
            BindGeneratedGraph(project);
        }

        private static string NormalizeProjectName(
            string? requestedName,
            DateTime now)
        {
            var normalized = requestedName?.Trim();

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return $"Project {now:yyyy-MM-dd HH:mm}";
            }

            return normalized.Length <= 120
                ? normalized
                : normalized[..120];
        }

        private static void SanitizeInputGraph(
            Project project)
        {
            project.FloorGrouping.Id = 0;
            project.FloorGrouping.ProjectId = 0;

            project.GlobalGrouping.Id = 0;
            project.GlobalGrouping.ProjectId = 0;

            project.InitialSettings.Id = 0;
            project.InitialSettings.ProjectId = 0;

            project.FuseBox.Id = 0;
            project.FuseBox.ProjectId = 0;

            // Generated schema is a server responsibility.
            // Ignore any device/connection graph supplied through a
            // legacy DTO.
            project.FuseBox.ComponentGroups =
                new List<FuseBoxComponentGroup>();

            project.FuseBox.CableConnections =
                new List<CableConnection>();

            foreach (var floor in project.Floors)
            {
                floor.Id = 0;
                floor.ProjectId = 0;

                foreach (var room in floor.Rooms)
                {
                    room.Id = 0;
                    room.FloorId = 0;

                    foreach (var consumer in room.Consumer)
                    {
                        consumer.Id = 0;
                        consumer.RoomId = 0;
                    }
                }
            }
        }

        private static void BindInputGraph(Project project)
        {
            project.FloorGrouping.Project = project;
            project.GlobalGrouping.Project = project;
            project.InitialSettings.Project = project;
            project.FuseBox.Project = project;

            foreach (var floor in project.Floors)
            {
                floor.Project = project;

                foreach (var room in floor.Rooms)
                {
                    room.Floor = floor;

                    foreach (var consumer in room.Consumer)
                    {
                        consumer.Room = room;
                    }
                }
            }
        }

        private static void BindGeneratedGraph(
            Project project)
        {
            var fuseBox = project.FuseBox;
            fuseBox.Project = project;

            foreach (var group in fuseBox.ComponentGroups)
            {
                group.Id = 0;
                group.FuseBoxUnitId = 0;
                group.FuseBoxUnit = fuseBox;

                foreach (var component in group.Components)
                {
                    component.Id = 0;
                    component.FuseBoxComponentGroupId = 0;
                    component.FuseBoxComponentGroup = group;

                    foreach (var port in component.Ports)
                    {
                        port.Id = 0;
                        port.ComponentId = 0;
                        port.Component = component;
                    }
                }
            }

            foreach (var connection in
                     fuseBox.CableConnections)
            {
                connection.Id = 0;
                connection.FuseBoxUnitId = 0;
                connection.FuseBoxUnit = fuseBox;

                connection.Cable.Id = 0;
                connection.Cable.ConnectionCableId = null;
                connection.Cable.Connection = connection;

                connection.CabelWay.Id = 0;
                connection.CabelWay.ConnectionPositionId = 0;
                connection.CabelWay.Connection = connection;
            }
        }

        private static void AttachReplacementGraph(
            Project existing,
            Project replacement)
        {
            existing.FloorGrouping =
                replacement.FloorGrouping;
            existing.FloorGrouping.Project = existing;

            existing.GlobalGrouping =
                replacement.GlobalGrouping;
            existing.GlobalGrouping.Project = existing;

            existing.InitialSettings =
                replacement.InitialSettings;
            existing.InitialSettings.Project = existing;

            existing.FuseBox = replacement.FuseBox;
            existing.FuseBox.Project = existing;

            existing.Floors = replacement.Floors;

            foreach (var floor in existing.Floors)
            {
                floor.Project = existing;

                foreach (var room in floor.Rooms)
                {
                    room.Floor = floor;

                    foreach (var consumer in room.Consumer)
                    {
                        consumer.Room = room;
                    }
                }
            }
        }

        private async Task DeleteExistingGraphAsync(
            int projectId,
            CancellationToken cancellationToken)
        {
            // Rooms and consumers are deleted through the Floor cascade.
            // Consumers must disappear before Fuse components because
            // Consumer.FuseId is a nullable FK without cascade delete.
            await _context.Floors
                .Where(floor =>
                    floor.ProjectId == projectId)
                .ExecuteDeleteAsync(cancellationToken);

            var fuseBoxId = await _context.FuseBoxes
                .AsNoTracking()
                .Where(fuseBox =>
                    fuseBox.ProjectId == projectId)
                .Select(fuseBox => (int?)fuseBox.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (fuseBoxId.HasValue)
            {
                // RCD.Electricals and Contactor.Electricals create
                // self-referencing shadow FKs in Component. Null them
                // before deleting all component groups.
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    UPDATE `Component` AS c
                    INNER JOIN `ComponentGroups` AS g
                        ON c.`FuseBoxComponentGroupId` = g.`Id`
                    SET c.`RCDId` = NULL,
                        c.`ContactorId` = NULL
                    WHERE g.`FuseBoxUnitId` = {fuseBoxId.Value}
                    """,
                    cancellationToken);

                // ComponentGroup -> Component -> Port cascades.
                await _context.ComponentGroups
                    .Where(group =>
                        group.FuseBoxUnitId ==
                        fuseBoxId.Value)
                    .ExecuteDeleteAsync(cancellationToken);

                // Connection -> Cable and Position cascades.
                await _context.Connections
                    .Where(connection =>
                        connection.FuseBoxUnitId ==
                        fuseBoxId.Value)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.FuseBoxes
                    .Where(fuseBox =>
                        fuseBox.Id == fuseBoxId.Value)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _context.FloorGroupings
                .Where(grouping =>
                    grouping.ProjectId == projectId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.GlobalGroupings
                .Where(grouping =>
                    grouping.ProjectId == projectId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.InitialSettings
                .Where(settings =>
                    settings.ProjectId == projectId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        private static void PrepareConsumerLoads(
            Project project)
        {
            if (project.InitialSettings == null)
            {
                throw new ArgumentException(
                    "Main project settings are missing.");
            }

            var voltage =
                project.InitialSettings.VoltageStandard;

            if (voltage != 220 && voltage != 230)
            {
                throw new ArgumentException(
                    "Single-phase consumer voltage must be " +
                    "220 or 230 V.");
            }

            int[] allowedBreakers =
            {
                2, 4, 6, 10, 16, 20, 25, 32
            };

            double totalPowerWatts = 0;

            foreach (var floor in project.Floors)
            {
                foreach (var room in floor.Rooms)
                {
                    foreach (var consumer in room.Consumer)
                    {
                        if (!consumer.PowerWatts.HasValue ||
                            !double.IsFinite(
                                consumer.PowerWatts.Value) ||
                            consumer.PowerWatts.Value < 0)
                        {
                            throw new ArgumentException(
                                "Enter a valid power value for " +
                                $"\"{consumer.Name}\".");
                        }

                        if (!allowedBreakers.Contains(
                            consumer.BreakerAmperage))
                        {
                            throw new ArgumentException(
                                "Invalid breaker rating for " +
                                $"\"{consumer.Name}\".");
                        }

                        if (consumer.RcdMilliAmps != 10 &&
                            consumer.RcdMilliAmps != 30)
                        {
                            throw new ArgumentException(
                                "Invalid RCD sensitivity for " +
                                $"\"{consumer.Name}\".");
                        }

                        consumer.Amper =
                            consumer.PowerWatts.Value /
                            voltage;

                        if (consumer.Amper >
                            consumer.BreakerAmperage)
                        {
                            throw new ArgumentException(
                                $"\"{consumer.Name}\": calculated " +
                                $"current {consumer.Amper:F2} A " +
                                "exceeds selected breaker " +
                                $"C{consumer.BreakerAmperage}.");
                        }

                        consumer.Room = room;

                        totalPowerWatts +=
                            consumer.PowerWatts.Value;
                    }
                }
            }

            if (!double.IsFinite(totalPowerWatts))
            {
                throw new ArgumentException(
                    "Total project power is outside the " +
                    "supported range.");
            }

            project.TotalPower = totalPowerWatts;
        }

        private static void GenerateConfiguration(
            Project project)
        {
            IProjectSettings settingsProvider =
                new ProjectSettingsProvider(project);

            IComponentFactory componentFactory =
                new ComponentFactory();

            IConnectionFactory connectionFactory =
                new ConnectionFactory();

            IFuseBoxManager fuseBoxManager =
                new FuseBoxCableConnectionManager(
                    project.FuseBox);

            IProjectGrouping projectGrouping =
                new ProjectGroupingProvider(project);

            IConsumerProvider consumerProvider =
                new ConsumerProvider(project);

            IDistributionService distributionService =
                new DistributionService(
                    settingsProvider,
                    projectGrouping,
                    consumerProvider,
                    componentFactory);

            var singlePhaseRules =
                new List<IShieldConfigurationRule>
                {
                    new IntroductoryModuleRule()
                };

            var threePhaseRules =
                new List<IShieldConfigurationRule>();

            var configurationService =
                new ConfigurationService(
                    settingsProvider,
                    componentFactory,
                    fuseBoxManager,
                    connectionFactory,
                    projectGrouping,
                    consumerProvider,
                    singlePhaseRules,
                    threePhaseRules,
                    distributionService);

            configurationService.GenerateConfiguration();
        }

        private static ProjectSummaryResponse ToSummary(
            Project project)
        {
            return new ProjectSummaryResponse
            {
                Id = project.Id,
                Name = project.Name ?? string.Empty,
                CreatedAtUtc = project.CreatedAtUtc,
                UpdatedAtUtc = project.UpdatedAtUtc
            };
        }
    }
}
