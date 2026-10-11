using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Billing
{
    public sealed class PaddleClient
    {
        private readonly HttpClient _httpClient;
        private readonly PaddleBillingOptions _options;

        public PaddleClient(
            HttpClient httpClient,
            IOptions<PaddleBillingOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task CheckConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(
                HttpMethod.Get,
                "event-types?per_page=1",
                payload: null,
                cancellationToken);
        }

        public async Task<string> CreateCustomerAsync(
            string email,
            int userId,
            CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(
                HttpMethod.Post,
                "customers",
                new
                {
                    email,
                    custom_data = new
                    {
                        fusebox_user_id = userId.ToString()
                    }
                },
                cancellationToken);

            return await ReadEntityIdAsync(response, cancellationToken);
        }

        public async Task<string> CreateProTransactionAsync(
            string customerId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(
                HttpMethod.Post,
                "transactions",
                new
                {
                    items = new[]
                    {
                        new
                        {
                            price_id = _options.ProPriceId,
                            quantity = 1
                        }
                    },
                    customer_id = customerId,
                    collection_mode = "automatic",
                    custom_data = new
                    {
                        fusebox_user_id = userId.ToString(),
                        fusebox_plan = "pro"
                    }
                },
                cancellationToken);

            return await ReadEntityIdAsync(response, cancellationToken);
        }

        public async Task<string> CreateCustomerPortalSessionAsync(
            string customerId,
            string? subscriptionId,
            CancellationToken cancellationToken = default)
        {
            object payload;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                payload = new { };
            }
            else
            {
                payload = new
                {
                    subscription_ids = new[] { subscriptionId }
                };
            }

            using var response = await SendAsync(
                HttpMethod.Post,
                $"customers/{Uri.EscapeDataString(customerId)}/portal-sessions",
                payload,
                cancellationToken);

            return await ReadPortalUrlAsync(
                response,
                subscriptionId,
                cancellationToken);
        }

        public async Task<PaddleSubscriptionSnapshot> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentException(
                    "Paddle subscription id is required.",
                    nameof(subscriptionId));
            }

            using var response = await SendAsync(
                HttpMethod.Get,
                $"subscriptions/{Uri.EscapeDataString(subscriptionId)}",
                payload: null,
                cancellationToken);

            return await ReadSubscriptionAsync(response, cancellationToken);
        }

        public async Task<IReadOnlyList<PaddleSubscriptionSnapshot>> ListSubscriptionsForCustomerAsync(
            string customerId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                throw new ArgumentException(
                    "Paddle customer id is required.",
                    nameof(customerId));
            }

            var relativePath =
                "subscriptions?customer_id=" +
                Uri.EscapeDataString(customerId) +
                "&price_id=" +
                Uri.EscapeDataString(_options.ProPriceId) +
                "&per_page=200";

            using var response = await SendAsync(
                HttpMethod.Get,
                relativePath,
                payload: null,
                cancellationToken);

            return await ReadSubscriptionsAsync(response, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string relativePath,
            object? payload,
            CancellationToken cancellationToken)
        {
            if (!_options.IsApiConfigured)
            {
                throw new InvalidOperationException(
                    "Paddle API is not configured.");
            }

            if (!_options.ApiKeyMatchesEnvironment)
            {
                throw new InvalidOperationException(
                    "Paddle API key does not match the configured environment.");
            }

            using var request = new HttpRequestMessage(
                method,
                new Uri(new Uri(_options.ApiBaseUrl), relativePath));

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey);
            request.Headers.TryAddWithoutValidation("Paddle-Version", "1");
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (payload != null)
            {
                request.Content = JsonContent.Create(payload);
            }

            var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var statusCode = response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);
            response.Dispose();

            var safeBody = body.Length <= 1200
                ? body
                : body[..1200];

            throw new InvalidOperationException(
                $"Paddle API returned HTTP {(int)statusCode}: {safeBody}");
        }

        private static async Task<string> ReadEntityIdAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("id", out var idElement) ||
                idElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(idElement.GetString()))
            {
                throw new InvalidOperationException(
                    "Paddle API response did not contain an entity id.");
            }

            return idElement.GetString()!;
        }

        private static async Task<PaddleSubscriptionSnapshot> ReadSubscriptionAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    "Paddle API response did not contain subscription data.");
            }

            return ParseSubscription(data);
        }

        private static async Task<IReadOnlyList<PaddleSubscriptionSnapshot>> ReadSubscriptionsAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "Paddle API response did not contain subscriptions.");
            }

            return data
                .EnumerateArray()
                .Select(ParseSubscription)
                .ToArray();
        }

        private static PaddleSubscriptionSnapshot ParseSubscription(
            JsonElement data)
        {
            var subscriptionId = RequiredString(data, "id");
            var customerId = RequiredString(data, "customer_id");
            var status = RequiredString(data, "status");
            var updatedAtUtc = RequiredDateTime(data, "updated_at");

            DateTime? periodStartUtc = null;
            DateTime? periodEndUtc = null;

            if (data.TryGetProperty("current_billing_period", out var period) &&
                period.ValueKind == JsonValueKind.Object)
            {
                periodStartUtc = TryReadDateTime(period, "starts_at");
                periodEndUtc = TryReadDateTime(period, "ends_at");
            }

            var cancelAtPeriodEnd =
                data.TryGetProperty("scheduled_change", out var scheduledChange) &&
                scheduledChange.ValueKind == JsonValueKind.Object &&
                scheduledChange.TryGetProperty("action", out var action) &&
                action.ValueKind == JsonValueKind.String &&
                string.Equals(
                    action.GetString(),
                    "cancel",
                    StringComparison.Ordinal);

            var priceIds = new HashSet<string>(StringComparer.Ordinal);

            if (data.TryGetProperty("items", out var items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (!item.TryGetProperty("price", out var price) ||
                        price.ValueKind != JsonValueKind.Object ||
                        !price.TryGetProperty("id", out var priceId) ||
                        priceId.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(priceId.GetString()))
                    {
                        continue;
                    }

                    priceIds.Add(priceId.GetString()!);
                }
            }

            int? fuseBoxUserId = null;
            if (data.TryGetProperty("custom_data", out var customData) &&
                customData.ValueKind == JsonValueKind.Object &&
                customData.TryGetProperty("fusebox_user_id", out var userId))
            {
                if (userId.ValueKind == JsonValueKind.Number &&
                    userId.TryGetInt32(out var numericUserId))
                {
                    fuseBoxUserId = numericUserId;
                }
                else if (userId.ValueKind == JsonValueKind.String &&
                    int.TryParse(userId.GetString(), out var stringUserId))
                {
                    fuseBoxUserId = stringUserId;
                }
            }

            return new PaddleSubscriptionSnapshot(
                subscriptionId,
                customerId,
                status,
                updatedAtUtc,
                periodStartUtc,
                periodEndUtc,
                cancelAtPeriodEnd,
                priceIds,
                fuseBoxUserId);
        }

        private static async Task<string> ReadPortalUrlAsync(
            HttpResponseMessage response,
            string? subscriptionId,
            CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("urls", out var urls))
            {
                throw new InvalidOperationException(
                    "Paddle API response did not contain customer portal URLs.");
            }

            if (!string.IsNullOrWhiteSpace(subscriptionId) &&
                urls.TryGetProperty("subscriptions", out var subscriptions) &&
                subscriptions.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidate in subscriptions.EnumerateArray())
                {
                    if (!candidate.TryGetProperty("id", out var idElement) ||
                        !string.Equals(
                            idElement.GetString(),
                            subscriptionId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // Paddle added view_subscription in API v1 in September
                    // 2026. Fall back to the general overview URL if an older
                    // response shape is encountered.
                    if (TryReadTrustedPortalUrl(
                        candidate,
                        "view_subscription",
                        out var viewSubscriptionUrl))
                    {
                        return viewSubscriptionUrl;
                    }
                }
            }

            if (urls.TryGetProperty("general", out var general) &&
                TryReadTrustedPortalUrl(
                    general,
                    "overview",
                    out var overviewUrl))
            {
                return overviewUrl;
            }

            throw new InvalidOperationException(
                "Paddle API response did not contain a trusted customer portal URL.");
        }

        private static bool TryReadTrustedPortalUrl(
            JsonElement parent,
            string propertyName,
            out string portalUrl)
        {
            portalUrl = string.Empty;

            if (!parent.TryGetProperty(propertyName, out var urlElement) ||
                urlElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var value = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(value) ||
                !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                !string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase) ||
                !uri.Host.EndsWith(
                    ".paddle.com",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            portalUrl = value;
            return true;
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
                    $"Paddle API response is missing {property}.");
            }

            return value.GetString()!;
        }

        private static DateTime RequiredDateTime(
            JsonElement element,
            string property)
        {
            return TryReadDateTime(element, property)
                ?? throw new InvalidOperationException(
                    $"Paddle API response has invalid {property}.");
        }

        private static DateTime? TryReadDateTime(
            JsonElement element,
            string property)
        {
            if (!element.TryGetProperty(property, out var value) ||
                value.ValueKind != JsonValueKind.String ||
                !DateTimeOffset.TryParse(value.GetString(), out var parsed))
            {
                return null;
            }

            return parsed.UtcDateTime;
        }
    }

    public sealed record PaddleSubscriptionSnapshot(
        string SubscriptionId,
        string CustomerId,
        string Status,
        DateTime UpdatedAtUtc,
        DateTime? CurrentPeriodStartUtc,
        DateTime? CurrentPeriodEndUtc,
        bool CancelAtPeriodEnd,
        IReadOnlySet<string> PriceIds,
        int? FuseBoxUserId);
}
