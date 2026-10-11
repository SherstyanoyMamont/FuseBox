using FuseBox.App.DataBase;
using FuseBox.App.Subscriptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Billing
{
    public sealed record BillingReadinessCheck(
        string Code,
        string Severity,
        bool Passed,
        string Message);

    public sealed record BillingProductionReadinessSnapshot(
        string Environment,
        bool BillingEnabled,
        bool ReadyForCurrentEnvironment,
        bool ReadyForProduction,
        bool ProviderCheckRequested,
        bool? ProviderApiReachable,
        DateTime CheckedAtUtc,
        IReadOnlyList<BillingReadinessCheck> Checks,
        int PaddleSubscriptions,
        int ActiveSubscriptions,
        int TrialingSubscriptions,
        int PastDueSubscriptions,
        int PausedSubscriptions,
        int CanceledSubscriptions,
        int UnprocessedWebhookEvents,
        DateTime? LastWebhookProcessedAtUtc,
        string? LastWebhookEventType);

    public sealed class BillingProductionReadinessService
    {
        private readonly AppDbContext _context;
        private readonly PaddleClient _paddle;
        private readonly PaddleBillingOptions _options;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public BillingProductionReadinessService(
            AppDbContext context,
            PaddleClient paddle,
            IOptions<PaddleBillingOptions> options,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _context = context;
            _paddle = paddle;
            _options = options.Value;
            _configuration = configuration;
            _environment = environment;
        }

        public void EnsureSafeForCheckout()
        {
            if (!_options.IsProduction)
            {
                return;
            }

            var blocking = BuildChecks(forProduction: true)
                .Where(check =>
                    check.Severity == "error" &&
                    !check.Passed)
                .Select(check => check.Code)
                .ToArray();

            if (blocking.Length > 0)
            {
                throw new BillingUnsafeProductionConfigurationException(
                    blocking);
            }
        }

        public async Task<BillingProductionReadinessSnapshot> GetSnapshotAsync(
            bool checkProvider,
            CancellationToken cancellationToken = default)
        {
            var checks = BuildChecks(forProduction: true).ToList();
            bool? providerReachable = null;

            if (checkProvider)
            {
                if (_options.IsApiConfigured &&
                    _options.ApiKeyMatchesEnvironment)
                {
                    try
                    {
                        await _paddle.CheckConnectionAsync(cancellationToken);
                        providerReachable = true;
                        checks.Add(new BillingReadinessCheck(
                            "provider_api_reachable",
                            "error",
                            true,
                            "Paddle API authentication succeeded."));
                    }
                    catch
                    {
                        providerReachable = false;
                        checks.Add(new BillingReadinessCheck(
                            "provider_api_reachable",
                            "error",
                            false,
                            "Paddle API authentication failed. Check the API key, environment, network, and permissions."));
                    }
                }
                else
                {
                    providerReachable = false;
                    checks.Add(new BillingReadinessCheck(
                        "provider_api_reachable",
                        "error",
                        false,
                        "Paddle API cannot be checked until API configuration is valid."));
                }
            }

            var providerEnvironment = _options.NormalizedEnvironment;

            var subscriptions = await _context.UserSubscriptions
                .AsNoTracking()
                .Where(item =>
                    item.Provider == SubscriptionProvider.Paddle &&
                    item.ProviderEnvironment == providerEnvironment)
                .Select(item => item.Status)
                .ToListAsync(cancellationToken);

            var foreignEnvironmentRows = await _context.UserSubscriptions
                .AsNoTracking()
                .CountAsync(
                    item =>
                        (item.Provider == SubscriptionProvider.Paddle ||
                         item.ProviderCustomerId != null ||
                         item.ProviderSubscriptionId != null) &&
                        item.ProviderEnvironment != providerEnvironment,
                    cancellationToken);

            checks.Add(new BillingReadinessCheck(
                "provider_environment_isolation",
                "warning",
                foreignEnvironmentRows == 0,
                foreignEnvironmentRows == 0
                    ? "No Paddle billing rows from another environment are present."
                    : $"{foreignEnvironmentRows} Paddle billing row(s) belong to another environment. They are isolated from current entitlements/reconciliation and will not be reused for checkout."));

            var lastWebhook = await _context.BillingWebhookEvents
                .AsNoTracking()
                .Where(item =>
                    item.Provider == "Paddle" &&
                    item.ProviderEnvironment == providerEnvironment &&
                    item.ProcessedAtUtc != null)
                .OrderByDescending(item => item.ProcessedAtUtc)
                .Select(item => new
                {
                    item.ProcessedAtUtc,
                    item.EventType
                })
                .FirstOrDefaultAsync(cancellationToken);

            var unprocessed = await _context.BillingWebhookEvents
                .AsNoTracking()
                .CountAsync(
                    item =>
                        item.Provider == "Paddle" &&
                        item.ProviderEnvironment == providerEnvironment &&
                        item.ProcessedAtUtc == null,
                    cancellationToken);

            var commonReady = checks
                .Where(check => check.Severity == "error")
                .All(check => check.Passed);

            // For live readiness, require an explicit provider check in this
            // request. We intentionally do not persist that result because API
            // credentials or network conditions may change after the check.
            var productionReady = commonReady &&
                checkProvider &&
                providerReachable == true;

            var currentReady = _options.IsProduction
                ? productionReady
                : BuildChecks(forProduction: false)
                    .Where(check => check.Severity == "error")
                    .All(check => check.Passed) &&
                  (!checkProvider || providerReachable == true);

            return new BillingProductionReadinessSnapshot(
                _options.NormalizedEnvironment,
                _options.Enabled,
                currentReady,
                productionReady,
                checkProvider,
                providerReachable,
                DateTime.UtcNow,
                checks,
                subscriptions.Count,
                subscriptions.Count(status => status == SubscriptionStatus.Active),
                subscriptions.Count(status => status == SubscriptionStatus.Trialing),
                subscriptions.Count(status => status == SubscriptionStatus.PastDue),
                subscriptions.Count(status => status == SubscriptionStatus.Paused),
                subscriptions.Count(status => status == SubscriptionStatus.Canceled),
                unprocessed,
                lastWebhook?.ProcessedAtUtc,
                lastWebhook?.EventType);
        }

        private IReadOnlyList<BillingReadinessCheck> BuildChecks(
            bool forProduction)
        {
            var checks = new List<BillingReadinessCheck>
            {
                Check(
                    "billing_enabled",
                    _options.Enabled,
                    "Paddle billing is enabled."),
                Check(
                    "known_environment",
                    _options.HasKnownEnvironment,
                    "Environment is explicitly 'sandbox' or 'production'."),
                Check(
                    "api_key_present",
                    !string.IsNullOrWhiteSpace(_options.ApiKey),
                    "Paddle API key is configured."),
                Check(
                    "api_key_environment",
                    _options.ApiKeyMatchesEnvironment,
                    "Paddle API key matches the selected environment."),
                Check(
                    "client_token_present",
                    !string.IsNullOrWhiteSpace(_options.ClientSideToken),
                    "Paddle client-side token is configured."),
                Check(
                    "client_token_environment",
                    _options.ClientSideTokenMatchesEnvironment,
                    "Paddle client-side token matches the selected environment."),
                Check(
                    "webhook_secret",
                    _options.HasValidWebhookSecretShape,
                    "Webhook destination secret is configured."),
                Check(
                    "pro_price_id",
                    _options.HasValidProPriceIdShape,
                    "Pro price ID is configured."),
                Check(
                    "reconciliation_enabled",
                    _options.ReconciliationEnabled,
                    "Background billing reconciliation is enabled."),
                Check(
                    "webhook_body_limit",
                    _options.MaxWebhookBodyBytes >= 16 * 1024 &&
                    _options.MaxWebhookBodyBytes <= 2 * 1024 * 1024,
                    "Webhook request body limit is within the hardened range."),
                new BillingReadinessCheck(
                    "webhook_signature_tolerance",
                    _options.WebhookToleranceSeconds >= 5 &&
                    _options.WebhookToleranceSeconds <= 60 ? "info" : "warning",
                    _options.WebhookToleranceSeconds >= 5 &&
                    _options.WebhookToleranceSeconds <= 60,
                    "Webhook timestamp tolerance should normally stay between 5 and 60 seconds.")
            };

            if (forProduction)
            {
                checks.Add(Check(
                    "production_environment_selected",
                    _options.IsProduction,
                    "Billing:Paddle:Environment is set to production."));
                checks.Add(Check(
                    "production_mode_confirmed",
                    _options.ProductionModeConfirmed,
                    "ProductionModeConfirmed explicitly acknowledges real payments."));
                checks.Add(Check(
                    "aspnet_environment_production",
                    _environment.IsProduction(),
                    "ASPNETCORE_ENVIRONMENT is Production."));
                checks.Add(Check(
                    "production_frontend_origins",
                    HasSafeProductionFrontendOrigins(),
                    "Frontend:AllowedOrigins contains only explicit HTTPS production origins."));
                checks.Add(new BillingReadinessCheck(
                    "live_notification_destination_manual",
                    "warning",
                    false,
                    "Manual check: create a separate live Paddle notification destination and enable all subscription lifecycle events."));
                checks.Add(new BillingReadinessCheck(
                    "webhook_ip_allowlist_manual",
                    "warning",
                    false,
                    "Manual check: enforce Paddle's current webhook IP allowlist at the production edge/reverse proxy where practical."));
            }

            return checks;
        }

        private bool HasSafeProductionFrontendOrigins()
        {
            var origins = _configuration
                .GetSection("Frontend:AllowedOrigins")
                .Get<string[]>()?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray() ?? Array.Empty<string>();

            if (origins.Length == 0)
            {
                return false;
            }

            foreach (var origin in origins)
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                    !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                    uri.IsLoopback ||
                    string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static BillingReadinessCheck Check(
            string code,
            bool passed,
            string message)
        {
            return new BillingReadinessCheck(
                code,
                "error",
                passed,
                message);
        }
    }

    public sealed class BillingUnsafeProductionConfigurationException :
        InvalidOperationException
    {
        public BillingUnsafeProductionConfigurationException(
            IReadOnlyList<string> failedChecks)
            : base(
                "Paddle production billing is blocked by failed readiness checks: " +
                string.Join(", ", failedChecks))
        {
            FailedChecks = failedChecks;
        }

        public IReadOnlyList<string> FailedChecks { get; }
    }
}
