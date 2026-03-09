using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class InvoiceWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceWorker> _logger;

    public InvoiceWorker(IServiceScopeFactory scopeFactory, ILogger<InvoiceWorker> logger)
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
                var retryService = scope.ServiceProvider.GetRequiredService<IBillingRetryService>();

                await invoiceService.GenerateDueInvoicesBatchAsync(100, stoppingToken);
                await retryService.ProcessPendingRetriesAsync(100, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InvoiceWorker failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
