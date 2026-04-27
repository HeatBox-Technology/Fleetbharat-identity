using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICommonDropdownService
{
    Task<List<DropdownDto>> GetAccountsAsync(string? search, int limit);
    Task<List<AccountDropdownDto>> GetAccountDropdownAsync(int? accountId, int? categoryId);
    Task<List<DropdownDto>> GetCategoriesAsync(string? search, int limit);
    Task<List<DropdownDto>> GetRolesAsync(int accountId, string? search, int limit);
    Task<List<DropdownGuidDto>> GetUsersAsync(int accountId, string? search, int limit);
    Task<List<AccountUserOptionDto>> GetAccountUsersAsync(int accountId);
    Task<List<DropdownDto>> GetCurrencyDropdownAsync();
    Task<List<DropdownDto>> GetFormModuleDropdownAsync();
    Task<List<DropdownDto>> GetVehicles(int accountId);

    Task<List<DropdownDto>> GetDevices(int accountId);
    Task<List<DropdownDto>> GetGeofences(
        int accountId,
        string? search,
        int limit);

    Task<List<DropdownDto>> GetSims(int accountId);
    Task<List<DriverDropdownDto>> GetDriversDropdownAsync(
        int? driverId,
        int? accountId,
        string? name,
        string? mobile);
    Task<List<VehicleTypeDropdownDto>> GetVehicleTypesAsync(int? id);

    Task<List<DropdownDto>> GetDeviceType();
    Task<List<DropdownDto>> GetTripTypes();
    Task<List<DropdownDto>> GetManufacture();
    Task<FormFilterConfigResponseDto?> GetFilterConfigByFormNameAsync(string formName);


}
