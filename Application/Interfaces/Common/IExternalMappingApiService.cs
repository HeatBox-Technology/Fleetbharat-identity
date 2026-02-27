using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public interface IExternalMappingApiService
{
    Task<bool> SendVehicleMappingAsync(ExternalVehicleMappingRequest request);
    Task<bool> SendGeofenceAsync(List<ExternalGeofenceRequest> payload, HttpMethod method);
}