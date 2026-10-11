using System.Security.Claims;
using FuseBox.App.Accounts;
using FuseBox.App.Contracts.Admin;
using FuseBox.App.Contracts.Common;
using FuseBox.App.DataBase;
using FuseBox.App.Models;
using FuseBox.App.Subscriptions;
using FuseBox.App.Usage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Administration
{
    [ApiController]
    [Authorize(Policy = "Admin")]
    [Route("api/admin")]
    public sealed class AdminController : ControllerBase
    {
        private const int DefaultPageSize = 50;
        private const int MaxPageSize = 100;

        private readonly AppDbContext _context;
        private readonly AccountService _accounts;
        private readonly SubscriptionService _subscriptions;
        private readonly EntitlementService _entitlements;
        private readonly UsageService _usage;

        public AdminController(
            AppDbContext context,
            AccountService accounts,
            SubscriptionService subscriptions,
            EntitlementService entitlements,
            UsageService usage)
        {
            _context = context;
            _accounts = accounts;
            _subscriptions = subscriptions;
            _entitlements = entitlements;
            _usage = usage;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<AdminSummaryResponse>> GetSummary(
            CancellationToken cancellationToken)
        {
            var users = _context.Users
                .AsNoTracking()
                .Where(user => user.PasswordHash != null);

            var userCount = await users.CountAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var proCount = await users.CountAsync(
                user =>
                    (user.Subscription != null &&
                     user.Subscription.Plan == PlanCode.Pro &&
                     (user.Subscription.Status == SubscriptionStatus.Active ||
                      user.Subscription.Status == SubscriptionStatus.Trialing ||
                      user.Subscription.Status == SubscriptionStatus.PastDue)) ||
                    (user.EntitlementOverride != null &&
                     user.EntitlementOverride.Plan == PlanCode.Pro &&
                     user.EntitlementOverride.ExpiresAtUtc > now),
                cancellationToken);

            var suspendedCount = await users.CountAsync(
                user => user.Status == UserStatus.Suspended,
                cancellationToken);

            var adminCount = await users.CountAsync(
                user => user.Role == UserRole.Admin,
                cancellationToken);

            var today = DateTime.UtcNow.Date;
            var aiRequestsToday = await _context.UsageCounters
                .AsNoTracking()
                .Where(counter =>
                    counter.Metric == UsageService.AiRequestMetric &&
                    counter.PeriodStartUtc == today)
                .SumAsync(
                    counter => (int?)counter.Count,
                    cancellationToken) ?? 0;

            return Ok(new AdminSummaryResponse
            {
                Users = userCount,
                ProUsers = proCount,
                FreeUsers = Math.Max(0, userCount - proCount),
                SuspendedUsers = suspendedCount,
                AdminUsers = adminCount,
                AiRequestsToday = aiRequestsToday
            });
        }

        [HttpGet("audit")]
        public async Task<ActionResult<AdminAuditPageResponse>> GetAudit(
            [FromQuery] string? search = null,
            [FromQuery] string? action = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.AdminAuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();
                var hasId = int.TryParse(value, out var searchedId);

                query = query.Where(item =>
                    item.ActorEmail.Contains(value) ||
                    (item.TargetEmail != null && item.TargetEmail.Contains(value)) ||
                    item.Action.Contains(value) ||
                    (item.Details != null && item.Details.Contains(value)) ||
                    (hasId && (item.ActorUserId == searchedId ||
                               item.TargetUserId == searchedId)));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                var normalizedAction = action.Trim().ToUpperInvariant();
                query = query.Where(item => item.Action == normalizedAction);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenByDescending(item => item.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new AdminAuditItemResponse
                {
                    Id = item.Id,
                    CreatedAtUtc = item.CreatedAtUtc,
                    ActorUserId = item.ActorUserId,
                    ActorEmail = item.ActorEmail,
                    Action = item.Action,
                    TargetUserId = item.TargetUserId,
                    TargetEmail = item.TargetEmail,
                    Details = item.Details
                })
                .ToArrayAsync(cancellationToken);

            return Ok(new AdminAuditPageResponse
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("users")]
        public async Task<ActionResult<AdminUsersPageResponse>> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null,
            [FromQuery] string? plan = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.Users
                .AsNoTracking()
                .Where(user => user.PasswordHash != null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToUpperInvariant();
                var hasId = int.TryParse(search.Trim(), out var searchedId);

                query = query.Where(user =>
                    user.NormalizedEmail.Contains(normalizedSearch) ||
                    (hasId && user.Id == searchedId));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (!TryParseRole(role, out var parsedRole))
                {
                    return BadRequest(Error(
                        "admin.invalid_role",
                        "Role must be 'user' or 'admin'."));
                }

                query = query.Where(user => user.Role == parsedRole);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!TryParseStatus(status, out var parsedStatus))
                {
                    return BadRequest(Error(
                        "admin.invalid_status",
                        "Status must be 'active' or 'suspended'."));
                }

                query = query.Where(user => user.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(plan))
            {
                if (!TryParsePlan(plan, out var parsedPlan))
                {
                    return BadRequest(Error(
                        "admin.invalid_plan",
                        "Plan must be 'free' or 'pro'."));
                }

                query = ApplyPlanFilter(
                    query,
                    parsedPlan,
                    DateTime.UtcNow);
            }

            var total = await query.CountAsync(cancellationToken);

            var users = await query
                .Include(user => user.Subscription)
                .Include(user => user.EntitlementOverride)
                .OrderByDescending(user => user.CreatedAtUtc)
                .ThenByDescending(user => user.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var ids = users.Select(user => user.Id).ToArray();
            var today = DateTime.UtcNow.Date;

            var usageByUser = ids.Length == 0
                ? new Dictionary<int, int>()
                : await _context.UsageCounters
                    .AsNoTracking()
                    .Where(counter =>
                        ids.Contains(counter.UserId) &&
                        counter.Metric == UsageService.AiRequestMetric &&
                        counter.PeriodStartUtc == today)
                    .ToDictionaryAsync(
                        counter => counter.UserId,
                        counter => counter.Count,
                        cancellationToken);

            var projectCounts = ids.Length == 0
                ? new Dictionary<int, int>()
                : await _context.Projects
                    .AsNoTracking()
                    .Where(project => ids.Contains(project.UserId))
                    .GroupBy(project => project.UserId)
                    .ToDictionaryAsync(
                        group => group.Key,
                        group => group.Count(),
                        cancellationToken);

            var items = users
                .Select(user => ToListItem(
                    user,
                    projectCounts.GetValueOrDefault(user.Id),
                    usageByUser.GetValueOrDefault(user.Id)))
                .ToArray();

            return Ok(new AdminUsersPageResponse
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("users/{id:int}")]
        public async Task<ActionResult<AdminUserDetailResponse>> GetUser(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        [HttpPost("users/{id:int}/suspend")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(
            int id,
            CancellationToken cancellationToken)
        {
            if (IsCurrentAdmin(id))
            {
                return Conflict(SelfMutationNotAllowed());
            }

            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            var before = ApiValue(user.Status);
            await RunAuditedAsync(
                user,
                AdminAuditActions.SuspendUser,
                $"status: {before} -> suspended",
                () => _accounts.SetStatusAsync(
                    user,
                    UserStatus.Suspended,
                    cancellationToken),
                cancellationToken);

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        [HttpPost("users/{id:int}/activate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            var before = ApiValue(user.Status);
            await RunAuditedAsync(
                user,
                AdminAuditActions.ActivateUser,
                $"status: {before} -> active",
                () => _accounts.SetStatusAsync(
                    user,
                    UserStatus.Active,
                    cancellationToken),
                cancellationToken);

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        [HttpPost("users/{id:int}/role")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(
            int id,
            [FromBody] AdminSetRoleRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParseRole(request.Role, out var role))
            {
                return BadRequest(Error(
                    "admin.invalid_role",
                    "Role must be 'user' or 'admin'."));
            }

            if (IsCurrentAdmin(id) && role != UserRole.Admin)
            {
                return Conflict(SelfMutationNotAllowed());
            }

            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            var before = ApiValue(user.Role);
            var action = role == UserRole.Admin
                ? AdminAuditActions.PromoteAdmin
                : AdminAuditActions.DemoteAdmin;

            await RunAuditedAsync(
                user,
                action,
                $"role: {before} -> {ApiValue(role)}",
                () => _accounts.SetRoleAsync(
                    user,
                    role,
                    cancellationToken),
                cancellationToken);

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        [HttpPost("users/{id:int}/entitlements")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetEntitlements(
            int id,
            [FromBody] AdminSetEntitlementsRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParsePlan(request.Plan, out var plan))
            {
                return BadRequest(Error(
                    "admin.invalid_plan",
                    "Plan must be 'free' or 'pro'."));
            }

            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            if (user.Subscription?.Provider == SubscriptionProvider.Paddle)
            {
                return Conflict(Error(
                    "admin.subscription_provider_managed",
                    "This subscription is managed by Paddle. " +
                    "Use the billing lifecycle or a future entitlement override instead."));
            }

            var before = ApiValue(user.Subscription?.Plan ?? PlanCode.Free);
            await RunAuditedAsync(
                user,
                AdminAuditActions.SetInternalPlan,
                $"billing plan: {before} -> {ApiValue(plan)}; provider: internal",
                async () =>
                {
                    await _subscriptions.SetInternalPlanAsync(
                        user,
                        plan,
                        cancellationToken);
                },
                cancellationToken);

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        [HttpPost("users/{id:int}/entitlement-override")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantEntitlementOverride(
            int id,
            [FromBody] AdminGrantEntitlementOverrideRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParsePlan(request.Plan, out var plan) ||
                plan != PlanCode.Pro)
            {
                return BadRequest(Error(
                    "admin.invalid_override_plan",
                    "Temporary entitlement override currently supports only the Pro plan."));
            }

            var expiresAtUtc = ToUtc(request.ExpiresAtUtc);
            if (expiresAtUtc <= DateTime.UtcNow)
            {
                return BadRequest(Error(
                    "admin.invalid_override_expiry",
                    "Entitlement override expiry must be in the future."));
            }

            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            var now = DateTime.UtcNow;
            var entitlementOverride = user.EntitlementOverride;

            if (entitlementOverride == null)
            {
                entitlementOverride = new UserEntitlementOverride
                {
                    User = user,
                    UserId = user.Id,
                    CreatedAtUtc = now
                };

                _context.UserEntitlementOverrides.Add(entitlementOverride);
                user.EntitlementOverride = entitlementOverride;
            }

            entitlementOverride.Plan = PlanCode.Pro;
            entitlementOverride.ExpiresAtUtc = expiresAtUtc;
            entitlementOverride.UpdatedAtUtc = now;
            user.UpdatedAtUtc = now;

            await RunAuditedAsync(
                user,
                AdminAuditActions.GrantEntitlement,
                $"plan: pro; expires: {expiresAtUtc:O}",
                () => Task.CompletedTask,
                cancellationToken);

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        [HttpPost("users/{id:int}/entitlement-override/remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEntitlementOverride(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            var previousExpiry = user.EntitlementOverride?.ExpiresAtUtc;

            await RunAuditedAsync(
                user,
                AdminAuditActions.RemoveEntitlement,
                previousExpiry.HasValue
                    ? $"temporary Pro removed; previous expiry: {previousExpiry.Value:O}"
                    : "temporary Pro remove requested; no override was present",
                () =>
                {
                    if (user.EntitlementOverride != null)
                    {
                        _context.UserEntitlementOverrides.Remove(
                            user.EntitlementOverride);
                        user.EntitlementOverride = null;
                        user.UpdatedAtUtc = DateTime.UtcNow;
                    }

                    return Task.CompletedTask;
                },
                cancellationToken);

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        [HttpPost("users/{id:int}/force-logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceLogout(
            int id,
            CancellationToken cancellationToken)
        {
            if (IsCurrentAdmin(id))
            {
                return Conflict(SelfMutationNotAllowed());
            }

            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            await RunAuditedAsync(
                user,
                AdminAuditActions.ForceLogout,
                "all existing authentication cookies invalidated",
                () => _accounts.ForceLogoutAsync(
                    user,
                    cancellationToken),
                cancellationToken);

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        [HttpPost("users/{id:int}/reset-ai-usage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAiUsage(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await LoadUserAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(UserNotFound());
            }

            var before = await _usage.GetAiUsageAsync(
                id,
                cancellationToken);

            await RunAuditedAsync(
                user,
                AdminAuditActions.ResetAiUsage,
                $"ai_request today: {before.Used} -> 0",
                () => _usage.ResetAiUsageAsync(
                    id,
                    cancellationToken),
                cancellationToken);

            return Ok(await ToDetailAsync(user, cancellationToken));
        }

        private async Task RunAuditedAsync(
            User target,
            string action,
            string? details,
            Func<Task> mutation,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            await mutation();

            var actorIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(actorIdValue, out var actorUserId))
            {
                throw new InvalidOperationException(
                    "The authenticated administrator has no valid user identifier claim.");
            }

            var actorEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(actorEmail))
            {
                throw new InvalidOperationException(
                    "The authenticated administrator has no email claim.");
            }

            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                ActorUserId = actorUserId,
                ActorEmail = actorEmail,
                Action = action,
                TargetUserId = target.Id,
                TargetEmail = target.Email,
                Details = details,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private async Task<User?> LoadUserAsync(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return null;
            }

            return await _context.Users
                .Include(user => user.Subscription)
                .Include(user => user.EntitlementOverride)
                .FirstOrDefaultAsync(
                    user => user.Id == id && user.PasswordHash != null,
                    cancellationToken);
        }

        private AdminUserListItemResponse ToListItem(
            User user,
            int projectCount,
            int aiUsed)
        {
            var entitlements = _entitlements.Resolve(
                user.Subscription,
                user.EntitlementOverride);
            var limit = entitlements.AiRequestsPerDay;
            var resetAt = DateTime.SpecifyKind(
                DateTime.UtcNow.Date.AddDays(1),
                DateTimeKind.Utc);

            return new AdminUserListItemResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = ApiValue(user.Role),
                Status = ApiValue(user.Status),
                Plan = ApiValue(entitlements.Plan),
                SubscriptionStatus = SubscriptionStatusValue(
                    user.Subscription?.Status ?? SubscriptionStatus.Active),
                SubscriptionProvider = ApiValue(
                    user.Subscription?.Provider ?? SubscriptionProvider.Internal),
                HasActiveEntitlementOverride =
                    user.EntitlementOverride is { Plan: PlanCode.Pro } &&
                    user.EntitlementOverride.ExpiresAtUtc > DateTime.UtcNow,
                EntitlementOverrideExpiresAtUtc =
                    user.EntitlementOverride?.ExpiresAtUtc,
                AiUsage = new AdminAiUsageResponse
                {
                    Used = Math.Max(0, aiUsed),
                    Limit = limit,
                    Remaining = Math.Max(0, limit - aiUsed),
                    ResetAtUtc = resetAt
                },
                Projects = projectCount,
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            };
        }

        private async Task<AdminUserDetailResponse> ToDetailAsync(
            User user,
            CancellationToken cancellationToken)
        {
            var entitlements = _entitlements.Resolve(
                user.Subscription,
                user.EntitlementOverride);
            var usage = await _usage.GetAiUsageAsync(
                user.Id,
                cancellationToken);
            var projectCount = await _context.Projects
                .AsNoTracking()
                .CountAsync(
                    project => project.UserId == user.Id,
                    cancellationToken);

            return new AdminUserDetailResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = ApiValue(user.Role),
                Status = ApiValue(user.Status),
                Plan = ApiValue(entitlements.Plan),
                SubscriptionPlan = ApiValue(
                    user.Subscription?.Plan ?? PlanCode.Free),
                SubscriptionStatus = SubscriptionStatusValue(
                    user.Subscription?.Status ?? SubscriptionStatus.Active),
                SubscriptionProvider = ApiValue(
                    user.Subscription?.Provider ?? SubscriptionProvider.Internal),
                CurrentPeriodStartUtc = user.Subscription?.CurrentPeriodStartUtc,
                CurrentPeriodEndUtc = user.Subscription?.CurrentPeriodEndUtc,
                CancelAtPeriodEnd = user.Subscription?.CancelAtPeriodEnd ?? false,
                EntitlementOverride = ToEntitlementOverrideResponse(
                    user.EntitlementOverride),
                AiUsage = new AdminAiUsageResponse
                {
                    Used = usage.Used,
                    Limit = usage.Limit,
                    Remaining = usage.Remaining,
                    ResetAtUtc = usage.ResetAtUtc
                },
                Features = new AdminFeaturesResponse
                {
                    OnePhase = entitlements.CanUseOnePhase,
                    ThreePhase = entitlements.CanUseThreePhase,
                    SchemaEditor = entitlements.CanUseSchemaEditor,
                    ThreeD = entitlements.CanUseThreeD,
                    VrMr = entitlements.CanUseVrMr
                },
                Projects = projectCount,
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            };
        }

        private bool IsCurrentAdmin(int targetUserId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var currentUserId) &&
                   currentUserId == targetUserId;
        }

        private static IQueryable<User> ApplyPlanFilter(
            IQueryable<User> query,
            PlanCode plan,
            DateTime nowUtc)
        {
            if (plan == PlanCode.Pro)
            {
                return query.Where(user =>
                    (user.Subscription != null &&
                     user.Subscription.Plan == PlanCode.Pro &&
                     (user.Subscription.Status == SubscriptionStatus.Active ||
                      user.Subscription.Status == SubscriptionStatus.Trialing ||
                      user.Subscription.Status == SubscriptionStatus.PastDue)) ||
                    (user.EntitlementOverride != null &&
                     user.EntitlementOverride.Plan == PlanCode.Pro &&
                     user.EntitlementOverride.ExpiresAtUtc > nowUtc));
            }

            return query.Where(user =>
                !(
                    (user.Subscription != null &&
                     user.Subscription.Plan == PlanCode.Pro &&
                     (user.Subscription.Status == SubscriptionStatus.Active ||
                      user.Subscription.Status == SubscriptionStatus.Trialing ||
                      user.Subscription.Status == SubscriptionStatus.PastDue)) ||
                    (user.EntitlementOverride != null &&
                     user.EntitlementOverride.Plan == PlanCode.Pro &&
                     user.EntitlementOverride.ExpiresAtUtc > nowUtc)
                ));
        }

        private static AdminEntitlementOverrideResponse?
            ToEntitlementOverrideResponse(
                UserEntitlementOverride? entitlementOverride)
        {
            if (entitlementOverride == null)
            {
                return null;
            }

            return new AdminEntitlementOverrideResponse
            {
                Plan = ApiValue(entitlementOverride.Plan),
                ExpiresAtUtc = entitlementOverride.ExpiresAtUtc,
                Active =
                    entitlementOverride.Plan == PlanCode.Pro &&
                    entitlementOverride.ExpiresAtUtc > DateTime.UtcNow
            };
        }

        private static DateTime ToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static bool TryParseRole(string? value, out UserRole role)
        {
            return Enum.TryParse(value?.Trim(), true, out role) &&
                   Enum.IsDefined(role);
        }

        private static bool TryParseStatus(string? value, out UserStatus status)
        {
            return Enum.TryParse(value?.Trim(), true, out status) &&
                   Enum.IsDefined(status);
        }

        private static bool TryParsePlan(string? value, out PlanCode plan)
        {
            return Enum.TryParse(value?.Trim(), true, out plan) &&
                   Enum.IsDefined(plan);
        }

        private static string ApiValue<TEnum>(TEnum value)
            where TEnum : struct, Enum
        {
            return value.ToString().ToLowerInvariant();
        }

        private static string SubscriptionStatusValue(
            SubscriptionStatus status)
        {
            return status == SubscriptionStatus.PastDue
                ? "past_due"
                : ApiValue(status);
        }

        private static ApiErrorResponse UserNotFound()
        {
            return Error(
                "admin.user_not_found",
                "User not found.");
        }

        private static ApiErrorResponse SelfMutationNotAllowed()
        {
            return Error(
                "admin.self_mutation_not_allowed",
                "An administrator cannot suspend, demote, or force-logout the current session account.");
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
