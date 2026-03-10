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

    public async Task<bool> SendVehicleMappingAsync(List<ExternalVehicleMappingRequest> payload)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/v1/mapping/vehicle-mapping",
            payload);

        return response.IsSuccessStatusCode;
    }
    public async Task<bool> SendGeofenceAsync(
    List<ExternalGeofenceRequest> payload,
    HttpMethod method)
    {
        HttpResponseMessage response;

        if (method == HttpMethod.Post)
        {
            response = await _http.PostAsJsonAsync(
                "/api/v1/mapping/geofence",
                payload);
        }
        else if (method == HttpMethod.Delete)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                "/api/v1/mapping/geofence")
            {
                Content = JsonContent.Create(payload)
            };

            response = await _http.SendAsync(request);
        }
        else
        {
            return false;
        }

        return response.IsSuccessStatusCode;
    }
    public async Task<bool> SendVehicleGeofenceMappingAsync(
     List<ExternalGeofenceMappingRequest> payload,
     HttpMethod method)
    {
        HttpResponseMessage response;

        if (method == HttpMethod.Post)
        {
            response = await _http.PostAsJsonAsync(
                "/api/v1/mapping/geofence-mapping",
                payload);
        }
        else if (method == HttpMethod.Delete)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                "/api/v1/mapping/geofence-mapping")
            {
                Content = JsonContent.Create(payload)
            };

            response = await _http.SendAsync(request);
        }
        else
        {
            return false;
        }

        return response.IsSuccessStatusCode;
    }

}
