using System.Threading.Tasks;

public interface IExternalMappingApiService
{
    Task<bool> SendVehicleMappingAsync(ExternalVehicleMappingRequest request);
}