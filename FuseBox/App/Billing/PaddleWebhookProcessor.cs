using System.Security.Cryptography;
using System.Text.Json;
using FuseBox.App.DataBase;
using FuseBox.App.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Billing
{
    public sealed record PaddleWebhookProcessResult(
        bool Duplicate,
        bool SubscriptionUpdated);

    public sealed class PaddleWebhookProcessor
    {
        private readonly AppDbContext _context;
        private readonly PaddleBillingOptions _options;
        private readonly ILogger<PaddleWebhookProcessor> _logger;

        public PaddleWebhookProcessor(
            AppDbContext context,
            IOptions<PaddleBillingOptions> options,
            ILogger<PaddleWebhookProcessor> logger)
        {
            _context = context;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<PaddleWebhookProcessResult> ProcessAsync(
            byte[] rawBody,
            CancellationToken cancellationToken = default)
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;

            var eventId = RequiredString(root, "event_id");
            var eventType = RequiredString(root, "event_type");
            var occurredAt = RequiredDateTime(root, "occurred_at");
            var providerEnvironment = _options.NormalizedEnvironment;

            if (await _context.BillingWebhookEvents
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.ProviderEnvironment == providerEnvironment &&
                        item.ProviderEventId == eventId,
                    cancellationToken))
            {
                return new PaddleWebhookProcessResult(true, false);
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            try
            {
                var webhook = new BillingWebhookEvent
                {
                    Provider = "Paddle",
                    ProviderEnvironment = providerEnvironment,
                    ProviderEventId = eventId,
                    EventType = eventType,
                    OccurredAtUtc = occurredAt,
                    PayloadSha256 = Convert.ToHexString(
                        SHA256.HashData(rawBody)),
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.BillingWebhookEvents.Add(webhook);
                await _context.SaveChangesAsync(cancellationToken);

                var subscriptionUpdated = false;
                if (eventType.StartsWith(
                    "subscription.",
                    StringComparison.Ordinal))
                {
                    if (!root.TryGetProperty("data", out var data) ||
                        data.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidOperationException(
                            "Paddle subscription webhook is missing data.");
                    }

                    subscriptionUpdated = await ApplySubscriptionAsync(
                        data,
                        occurredAt,
                        cancellationToken);
                }

                webhook.ProcessedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new PaddleWebhookProcessResult(
                    false,
                    subscriptionUpdated);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);

                if (await _context.BillingWebhookEvents
                    .AsNoTracking()
                    .AnyAsync(
                        item =>
                            item.ProviderEnvironment == providerEnvironment &&
                            item.ProviderEventId == eventId,
                        cancellationToken))
                {
                    return new PaddleWebhookProcessResult(true, false);
                }

                throw;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<bool> ApplySubscriptionAsync(
            JsonElement data,
            DateTime eventOccurredAtUtc,
            CancellationToken cancellationToken)
        {
            var providerSubscriptionId = RequiredString(data, "id");
            var providerCustomerId = RequiredString(data, "customer_id");
            var status = MapStatus(RequiredString(data, "status"));

            var providerEnvironment = _options.NormalizedEnvironment;

            var existingBySubscription = await _context.UserSubscriptions
                .Include(item => item.User)
                .FirstOrDefaultAsync(
                    item =>
                        item.ProviderEnvironment == providerEnvironment &&
                        item.ProviderSubscriptionId == providerSubscriptionId,
                    cancellationToken);

            var existing = existingBySubscription ??
                await _context.UserSubscriptions
                    .Include(item => item.User)
                    .FirstOrDefaultAsync(
                        item =>
                            item.ProviderEnvironment == providerEnvironment &&
                            item.ProviderCustomerId == providerCustomerId,
                        cancellationToken);

            var userId = TryGetFuseBoxUserId(data);
            var user = existing?.User;

            if (user == null && userId.HasValue)
            {
                user = await _context.Users
                    .Include(candidate => candidate.Subscription)
                    .SingleOrDefaultAsync(
                        candidate => candidate.Id == userId.Value,
                        cancellationToken);
            }

            if (user == null)
            {
                _logger.LogWarning(
                    "Ignoring Paddle subscription {SubscriptionId}: no FuseBox user mapping.",
                    providerSubscriptionId);
                return false;
            }

            var hasConfiguredProPrice = HasConfiguredProPrice(data);

            // A price change on the exact subscription we already know about
            // must be able to revoke paid Pro. Do not use a customer-id-only
            // match for this: the same Paddle customer may own another
            // unrelated subscription.
            if (!hasConfiguredProPrice && existingBySubscription == null)
            {
                _logger.LogInformation(
                    "Ignoring Paddle subscription {SubscriptionId}: it is not the known FuseBox Pro subscription and does not contain the configured Pro price.",
                    providerSubscriptionId);
                return false;
            }

            var now = DateTime.UtcNow;
            var subscription = user.Subscription ?? existing;

            var sameProviderEnvironment =
                string.Equals(
                    subscription?.ProviderEnvironment,
                    providerEnvironment,
                    StringComparison.OrdinalIgnoreCase);

            if (sameProviderEnvironment &&
                subscription is { ProviderSubscriptionId: not null } &&
                !string.Equals(
                    subscription.ProviderSubscriptionId,
                    providerSubscriptionId,
                    StringComparison.Ordinal) &&
                subscription.Status != SubscriptionStatus.Canceled)
            {
                _logger.LogWarning(
                    "Ignoring additional Paddle subscription {SubscriptionId} for user {UserId}; current subscription {CurrentSubscriptionId} is not canceled.",
                    providerSubscriptionId,
                    user.Id,
                    subscription.ProviderSubscriptionId);
                return false;
            }

            if (subscription == null)
            {
                subscription = new UserSubscription
                {
                    User = user,
                    CreatedAtUtc = now
                };
                _context.UserSubscriptions.Add(subscription);
                user.Subscription = subscription;
            }

            if (sameProviderEnvironment &&
                subscription.ProviderUpdatedAtUtc.HasValue &&
                eventOccurredAtUtc <= subscription.ProviderUpdatedAtUtc.Value)
            {
                return false;
            }

            subscription.Provider = SubscriptionProvider.Paddle;
            subscription.ProviderEnvironment = providerEnvironment;
            subscription.ProviderCustomerId = providerCustomerId;
            subscription.ProviderSubscriptionId = providerSubscriptionId;
            subscription.ProviderUpdatedAtUtc = eventOccurredAtUtc;
            subscription.Plan = hasConfiguredProPrice
                ? PlanCode.Pro
                : PlanCode.Free;
            subscription.Status = status;
            subscription.CurrentPeriodStartUtc = TryGetPeriodDate(
                data,
                "starts_at");
            subscription.CurrentPeriodEndUtc = TryGetPeriodDate(
                data,
                "ends_at");
            subscription.CancelAtPeriodEnd = IsCancellationScheduled(data);
            subscription.UpdatedAtUtc = now;

            return true;
        }

        private bool HasConfiguredProPrice(JsonElement data)
        {
            if (string.IsNullOrWhiteSpace(_options.ProPriceId) ||
                !data.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("price", out var price) ||
                    price.ValueKind != JsonValueKind.Object ||
                    !price.TryGetProperty("id", out var priceId) ||
                    priceId.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (string.Equals(
                    priceId.GetString(),
                    _options.ProPriceId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int? TryGetFuseBoxUserId(JsonElement data)
        {
            if (!data.TryGetProperty("custom_data", out var customData) ||
                customData.ValueKind != JsonValueKind.Object ||
                !customData.TryGetProperty(
                    "fusebox_user_id",
                    out var userIdElement))
            {
                return null;
            }

            if (userIdElement.ValueKind == JsonValueKind.Number &&
                userIdElement.TryGetInt32(out var numberId))
            {
                return numberId;
            }

            return userIdElement.ValueKind == JsonValueKind.String &&
                int.TryParse(userIdElement.GetString(), out var stringId)
                    ? stringId
                    : null;
        }

        private static DateTime? TryGetPeriodDate(
            JsonElement data,
            string property)
        {
            if (!data.TryGetProperty(
                    "current_billing_period",
                    out var period) ||
                period.ValueKind != JsonValueKind.Object ||
                !period.TryGetProperty(property, out var value) ||
                value.ValueKind != JsonValueKind.String ||
                !DateTimeOffset.TryParse(value.GetString(), out var parsed))
            {
                return null;
            }

            return parsed.UtcDateTime;
        }

        private static bool IsCancellationScheduled(JsonElement data)
        {
            return data.TryGetProperty("scheduled_change", out var change) &&
                change.ValueKind == JsonValueKind.Object &&
                change.TryGetProperty("action", out var action) &&
                action.ValueKind == JsonValueKind.String &&
                string.Equals(
                    action.GetString(),
                    "cancel",
                    StringComparison.Ordinal);
        }

        private static SubscriptionStatus MapStatus(string status)
        {
            return status switch
            {
                "active" => SubscriptionStatus.Active,
                "trialing" => SubscriptionStatus.Trialing,
                "past_due" => SubscriptionStatus.PastDue,
                "paused" => SubscriptionStatus.Paused,
                "canceled" => SubscriptionStatus.Canceled,
                _ => SubscriptionStatus.Inactive
            };
        }

        private static string RequiredString(
            JsonElement element,
            string property)
        {
            if (!element.TryGetProperty(property, out var value) ||
                value.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new InvalidOperationException(
                    $"Paddle webhook is missing {property}.");
            }

            return value.GetString()!;
        }

        private static DateTime RequiredDateTime(
            JsonElement element,
            string property)
        {
            var value = RequiredString(element, property);
            if (!DateTimeOffset.TryParse(value, out var parsed))
            {
                throw new InvalidOperationException(
                    $"Paddle webhook has invalid {property}.");
            }

            return parsed.UtcDateTime;
        }
    }
}
