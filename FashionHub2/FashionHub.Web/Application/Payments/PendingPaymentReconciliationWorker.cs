using Microsoft.Extensions.Options;

namespace FashionHub.Web.Application.Payments;

public sealed class PendingPaymentReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOptionsMonitor<VnPayOptions> options;
    private readonly ILogger<PendingPaymentReconciliationWorker> logger;

    public PendingPaymentReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<VnPayOptions> options,
        ILogger<PendingPaymentReconciliationWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.options = options;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<IPendingPaymentReconciliationService>();
                var summary = await service.ReconcileAsync(stoppingToken);
                if (summary.Checked > 0)
                {
                    logger.LogInformation(
                        "VNPAY reconciliation checked {Checked}, paid {Paid}, expired {Expired}, unchanged {Unchanged}",
                        summary.Checked,
                        summary.Paid,
                        summary.Expired,
                        summary.Unchanged);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "VNPAY reconciliation cycle failed");
            }

            var interval = Math.Clamp(
                options.CurrentValue.ReconciliationIntervalMinutes,
                1,
                1440);
            await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
        }
    }
}
