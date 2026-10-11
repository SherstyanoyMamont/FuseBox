using System.Security.Claims;
using FuseBox.App.Billing;
using FuseBox.App.Contracts.Accounts;
using FuseBox.App.DataBase;
using FuseBox.App.Subscriptions;
using FuseBox.App.Usage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Accounts
{
    [ApiController]
    [Authorize]
    [Route("api/account")]
    public sealed class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EntitlementService _entitlements;
        private readonly UsageService _usage;
        private readonly PaddleBillingOptions _billingOptions;

        public AccountController(
            AppDbContext context,
            EntitlementService entitlements,
            UsageService usage,
            IOptions<PaddleBillingOptions> billingOptions)
        {
            _context = context;
            _entitlements = entitlements;
            _usage = usage;
            _billingOptions = billingOptions.Value;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(
            CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var account = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new
                {
                    user.Email,
                    user.Role,
                    user.Status,
                    user.CreatedAtUtc,
                    ProjectCount = user.Projects.Count,
                    SubscriptionStatus = user.Subscription == null
                        ? (SubscriptionStatus?)null
                        : user.Subscription.Status,
                    SubscriptionProvider = user.Subscription == null
                        ? (SubscriptionProvider?)null
                        : user.Subscription.Provider,
                    CurrentPeriodStartUtc = user.Subscription == null
                        ? null
                        : user.Subscription.CurrentPeriodStartUtc,
                    CurrentPeriodEndUtc = user.Subscription == null
                        ? null
                        : user.Subscription.CurrentPeriodEndUtc,
                    CancelAtPeriodEnd = user.Subscription != null &&
                        user.Subscription.CancelAtPeriodEnd,
                    ProviderCustomerId = user.Subscription == null
                        ? null
                        : user.Subscription.ProviderCustomerId
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (account == null)
            {
                return Unauthorized();
            }

            var entitlements = await _entitlements.GetForUserAsync(
                userId,
                cancellationToken);

            var aiUsage = await _usage.GetAiUsageAsync(
                userId,
                cancellationToken);

            return Ok(new AccountOverviewResponse
            {
                Profile = new AccountProfileResponse
                {
                    Email = account.Email,
                    Role = account.Role.ToString().ToLowerInvariant(),
                    Status = account.Status.ToString().ToLowerInvariant(),
                    CreatedAtUtc = account.CreatedAtUtc
                },
                Subscription = new AccountSubscriptionResponse
                {
                    Plan = entitlements.Plan
                        .ToString()
                        .ToLowerInvariant(),
                    Status = SubscriptionStatusToApiValue(
                        account.SubscriptionStatus ??
                        SubscriptionStatus.Active),
                    Provider = (account.SubscriptionProvider ??
                        SubscriptionProvider.Internal)
                        .ToString()
                        .ToLowerInvariant(),
                    CurrentPeriodStartUtc = account.CurrentPeriodStartUtc,
                    CurrentPeriodEndUtc = account.CurrentPeriodEndUtc,
                    CancelAtPeriodEnd = account.CancelAtPeriodEnd
                },
                Usage = new AccountUsageResponse
                {
                    AiRequests = new AccountAiUsageResponse
                    {
                        Used = aiUsage.Used,
                        Limit = aiUsage.Limit,
                        Remaining = aiUsage.Remaining,
                        ResetAtUtc = aiUsage.ResetAtUtc
                    }
                },
                Features = new AccountFeaturesResponse
                {
                    OnePhase = entitlements.CanUseOnePhase,
                    ThreePhase = entitlements.CanUseThreePhase,
                    SchemaEditor = entitlements.CanUseSchemaEditor,
                    ThreeD = entitlements.CanUseThreeD,
                    VrMr = entitlements.CanUseVrMr
                },
                Projects = new AccountProjectsResponse
                {
                    Count = account.ProjectCount
                },
                Billing = new AccountBillingResponse
                {
                    CheckoutAvailable = _billingOptions.IsCheckoutConfigured,
                    PortalAvailable =
                        _billingOptions.IsPortalConfigured &&
                        account.SubscriptionProvider ==
                            SubscriptionProvider.Paddle &&
                        !string.IsNullOrWhiteSpace(
                            account.ProviderCustomerId),
                    Provider = "paddle",
                    Environment = _billingOptions.IsSandbox
                        ? "sandbox"
                        : "production"
                }
            });
        }

        private static string SubscriptionStatusToApiValue(
            SubscriptionStatus status)
        {
            return status switch
            {
                SubscriptionStatus.PastDue => "past_due",
                _ => status.ToString().ToLowerInvariant()
            };
        }
    }
}
