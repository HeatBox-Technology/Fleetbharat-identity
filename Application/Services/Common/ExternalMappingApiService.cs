using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class ExternalMappingApiService : IExternalMappingApiService
{
    private readonly HttpClient _http;

    public ExternalMappingApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> SendVehicleMappingAsync(ExternalVehicleMappingRequest request)
    {
        var payload = new List<ExternalVehicleMappingRequest> { request };

        var response = await _http.PostAsJsonAsync(
            "/api/v1/mapping/vehicle-mapping",
            payload);

        return response.IsSuccessStatusCode;
    }
}