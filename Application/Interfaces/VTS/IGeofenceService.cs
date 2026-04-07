using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGeofenceService
{
    Task<int> CreateAsync(CreateGeofenceDto dto);
    Task<int> CreateByLocationAsync(CreateGeofenceByLocationDto dto);

    Task<GeofenceDto?> GetByIdAsync(int id);
    Task<GeofenceListUiResponseDto> GetZones(int page, int pageSize, int? accountId, string? search = null);
    Task<PagedResultDto<GeofenceDto>> GetPagedAsync(int page, int pageSize, int? accountId, string? search = null);
    Task<bool> UpdateAsync(int id, UpdateGeofenceDto dto);
    Task<bool> UpdateStatusAsync(int id, bool isEnabled);
    Task<bool> DeleteAsync(int id);
    Task<List<GeofenceDto>> BulkCreateAsync(List<CreateGeofenceDto> items);
}