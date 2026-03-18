using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

public class VtsExternalApiSyncDispatcher : IVtsExternalApiSyncDispatcher
{
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8)
    };

    private readonly IExternalMappingApiService _externalApiService;
    private readonly IExternalApiLogRepository _logRepository;
    private readonly ILogger<VtsExternalApiSyncDispatcher> _logger;

    public VtsExternalApiSyncDispatcher(
        IExternalMappingApiService externalApiService,
        IExternalApiLogRepository logRepository,
        ILogger<VtsExternalApiSyncDispatcher> logger)
    {
        _externalApiService = externalApiService;
        _logRepository = logRepository;
        _logger = logger;
    }

    public Task SyncGeofenceAsync(string payloadJson, CancellationToken ct = default) =>
        ExecuteWithRetryAsync(
            payloadJson,
            (envelope, _) => _externalApiService.SendGeofenceAsync(
                DeserializePayload<ExternalGeofenceRequest>(envelope.PayloadJson),
                ResolveHttpMethod(envelope.Operation)),
            ct);

    public Task SyncVehicleDeviceMappingAsync(string payloadJson, CancellationToken ct = default) =>
        ExecuteWithRetryAsync(
            payloadJson,
            (envelope, _) => _externalApiService.SendVehicleMappingAsync(
                DeserializePayload<ExternalVehicleMappingRequest>(envelope.PayloadJson)),
            ct);

    public Task SyncVehicleGeofenceMappingAsync(string payloadJson, CancellationToken ct = default) =>
        ExecuteWithRetryAsync(
            payloadJson,
            (envelope, _) => _externalApiService.SendVehicleGeofenceMappingAsync(
                DeserializePayload<ExternalGeofenceMappingRequest>(envelope.PayloadJson),
                ResolveHttpMethod(envelope.Operation)),
            ct);

    private async Task ExecuteWithRetryAsync(
        string payloadJson,
        Func<VtsExternalSyncEnvelope, CancellationToken, Task<bool>> sendAsync,
        CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<VtsExternalSyncEnvelope>(payloadJson)
            ?? throw new InvalidOperationException("Invalid external sync envelope payload.");

        var retryContext = new Context();
        var policy = Policy<bool>
            .Handle<Exception>()
            .OrResult(success => !success)
            .WaitAndRetryAsync(
                RetryDelays,
                async (outcome, delay, retryNumber, context) =>
                {
                    context["retryCount"] = retryNumber;
                    var response = outcome.Exception?.Message ?? "External API returned an unsuccessful status.";
                    await MarkLogAsync(envelope.ExternalApiLogId, ExternalApiLogStatus.Pending, retryNumber, response, ct);
                    _logger.LogWarning(
                        "External API retry scheduled. LogId={LogId}, Retry={Retry}, DelaySeconds={DelaySeconds}, Response={Response}",
                        envelope.ExternalApiLogId,
                        retryNumber,
                        delay.TotalSeconds,
                        response);
                });

        try
        {
            var result = await policy.ExecuteAsync(
                async (_, token) => await sendAsync(envelope, token),
                retryContext,
                ct);

            var retryCount = retryContext.TryGetValue("retryCount", out var value) ? Convert.ToInt32(value) : 0;
            var response = result ? "External API sync succeeded." : "External API returned an unsuccessful status.";
            await MarkLogAsync(envelope.ExternalApiLogId, ExternalApiLogStatus.Success, retryCount, response, ct);
        }
        catch (Exception ex)
        {
            var retryCount = retryContext.TryGetValue("retryCount", out var value) ? Convert.ToInt32(value) : RetryDelays.Length;
            await MarkLogAsync(envelope.ExternalApiLogId, ExternalApiLogStatus.Failed, retryCount, ex.Message, ct);
            throw;
        }
    }

    private async Task MarkLogAsync(long logId, string status, int retryCount, string? response, CancellationToken ct)
    {
        var log = await _logRepository.GetByIdAsync(logId, ct);
        if (log == null)
            return;

        log.Status = status;
        log.RetryCount = retryCount;
        log.Response = Trim(response);
        log.LastRetryAt = DateTime.UtcNow;
        await _logRepository.SaveChangesAsync(ct);
    }

    private static List<T> DeserializePayload<T>(string payloadJson)
    {
        return JsonSerializer.Deserialize<List<T>>(payloadJson)
            ?? throw new InvalidOperationException("Invalid external payload.");
    }

    private static HttpMethod ResolveHttpMethod(string operation)
    {
        return string.Equals(operation, HttpMethod.Delete.Method, StringComparison.OrdinalIgnoreCase)
            ? HttpMethod.Delete
            : HttpMethod.Post;
    }

    private static string? Trim(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        const int maxLength = 1800;
        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
