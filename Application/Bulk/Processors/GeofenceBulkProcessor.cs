using System.Text.Json;
using System.Threading.Tasks;

public class GeofenceBulkProcessor : IBulkModuleProcessor
{
    public string ModuleName => "GEOFENCE";

    private readonly IGeofenceService _service;

    public GeofenceBulkProcessor(IGeofenceService service)
    {
        _service = service;
    }

    public async Task ProcessAsync(string payloadJson)
    {
        var dto = JsonSerializer.Deserialize<CreateGeofenceDto>(payloadJson);

        await _service.CreateAsync(dto);
    }
}