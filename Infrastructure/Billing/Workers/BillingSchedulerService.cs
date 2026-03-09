using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class BillingSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BillingSchedulerService> _logger;

    public BillingSchedulerService(IServiceScopeFactory scopeFactory, ILogger<BillingSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var invoiceService = scope.ServiceProvider.GetRequiredService<IBillingInvoiceService>();

                var generated = await invoiceService.GenerateDueInvoicesBatchAsync(200, stoppingToken);
                if (generated > 0)
                {
                    _logger.LogInformation("BillingScheduler generated {Count} invoices.", generated);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BillingScheduler failed.");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
