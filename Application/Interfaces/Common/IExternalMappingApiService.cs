using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public interface IExternalMappingApiService
{
    Task<bool> SendVehicleMappingAsync(List<ExternalVehicleMappingRequest> payload);
    Task<bool> SendGeofenceAsync(List<ExternalGeofenceRequest> payload, HttpMethod method);
    Task<bool> SendVehicleGeofenceMappingAsync(
         List<ExternalGeofenceMappingRequest> payload,
         HttpMethod method);
}
