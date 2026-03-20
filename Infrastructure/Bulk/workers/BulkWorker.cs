using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class BulkWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BulkQueue _queue;

    public BulkWorker(IServiceScopeFactory scopeFactory, BulkQueue queue)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IBulkService>();

            await service.ProcessJobAsync(jobId);
        }
    }
}