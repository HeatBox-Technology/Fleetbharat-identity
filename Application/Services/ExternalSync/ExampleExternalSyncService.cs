using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class ExampleExternalSyncService : IExampleExternalSyncService
{
    private readonly ILogger<ExampleExternalSyncService> _logger;

    public ExampleExternalSyncService(ILogger<ExampleExternalSyncService> logger)
    {
        _logger = logger;
    }

    public Task SyncAsync(string payloadJson, CancellationToken ct = default)
    {
        // Example implementation: replace with actual external API call.
        _logger.LogInformation("Example external sync called with payload: {Payload}", payloadJson);
        return Task.CompletedTask;
    }
}
