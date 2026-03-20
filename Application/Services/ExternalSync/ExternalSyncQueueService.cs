using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class ExternalSyncQueueService : IExternalSyncQueueService
{
    private const int MaxPayloadLength = 2048;
    private readonly IExternalSyncRepository _repo;

    public ExternalSyncQueueService(IExternalSyncRepository repo)
    {
        _repo = repo;
    }

    public async Task EnqueueAsync(ExternalSyncQueueCreateRequest request, CancellationToken ct = default)
    {
        var config = await _repo.GetActiveConfigAsync(request.ModuleName, ct);
        if (config == null)
            return;

        var queueItem = new external_sync_queue
        {
            ModuleName = request.ModuleName,
            EntityId = request.EntityId,
            PayloadJson = request.PreservePayload
                ? request.PayloadJson
                : BuildCompactPayload(request.EntityId, request.PayloadJson),
            Status = ExternalSyncStatus.Pending,
            RetryCount = 0,
            NextRetryTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddQueueAsync(queueItem, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static string BuildCompactPayload(string entityId, string? payloadJson)
    {
        if (!string.IsNullOrWhiteSpace(payloadJson) && payloadJson.Length <= MaxPayloadLength)
        {
            return payloadJson;
        }

        return JsonSerializer.Serialize(new
        {
            entityId,
            hasPayload = !string.IsNullOrWhiteSpace(payloadJson),
            compacted = true
        });
    }
}
