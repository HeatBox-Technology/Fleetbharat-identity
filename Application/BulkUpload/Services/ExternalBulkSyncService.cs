using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class ExternalBulkSyncService : IExternalBulkSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalBulkSyncService> _logger;

    public ExternalBulkSyncService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ExternalBulkSyncService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SyncAsync(string moduleKey, IReadOnlyCollection<object> batch, CancellationToken ct = default)
    {
        var baseUrl = _configuration["BulkUpload:ExternalSyncBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("External sync skipped for {ModuleKey} because BulkUpload:ExternalSyncBaseUrl is not configured.", moduleKey);
            return;
        }

        var endpoint = $"{baseUrl.TrimEnd('/')}/bulk-sync/{moduleKey}";
        var payload = new { moduleKey, count = batch.Count, items = batch };

        var response = await _httpClient.PostAsJsonAsync(endpoint, payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("External sync failed for {ModuleKey}. Status={StatusCode}, Body={Body}", moduleKey, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }
}
