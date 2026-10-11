using Microsoft.Extensions.Options;

namespace FuseBox.App.Billing
{
    public sealed class BillingReconciliationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PaddleBillingOptions _options;
        private readonly ILogger<BillingReconciliationHostedService> _logger;

        public BillingReconciliationHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<PaddleBillingOptions> options,
            ILogger<BillingReconciliationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            if (!_options.ReconciliationEnabled)
            {
                _logger.LogInformation(
                    "Paddle billing reconciliation background job is disabled.");
                return;
            }

            if (_options.ReconciliationStartupDelay > TimeSpan.Zero)
            {
                await Task.Delay(
                    _options.ReconciliationStartupDelay,
                    stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);

                await Task.Delay(
                    _options.ReconciliationInterval,
                    stoppingToken);
            }
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            if (!_options.IsReconciliationConfigured)
            {
                _logger.LogDebug(
                    "Skipping Paddle reconciliation because billing API configuration is incomplete.");
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reconciliation = scope.ServiceProvider
                    .GetRequiredService<BillingReconciliationService>();

                var result = await reconciliation.ReconcileAllAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Paddle reconciliation completed. Checked={Checked}, Updated={Updated}, Unchanged={Unchanged}, Failed={Failed}.",
                    result.Checked,
                    result.Updated,
                    result.Unchanged,
                    result.Failed);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Normal application shutdown.
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Paddle billing reconciliation background run failed.");
            }
        }
    }
}
