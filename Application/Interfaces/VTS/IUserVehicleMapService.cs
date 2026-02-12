using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


/// <summary>
/// Service for mapping users to vehicles (vehicle-wise login/filtering).
/// Provides CRUD operations for user-vehicle access mapping.
/// </summary>
public interface IUserVehicleMapService
{
    /// <summary>Get all user-vehicle mappings</summary>
    Task<IEnumerable<UserVehicleMapDto>> GetAllAsync();
    /// <summary>Get a user-vehicle mapping by ID</summary>
    Task<UserVehicleMapDto?> GetByIdAsync(long id);
    /// <summary>Create a new user-vehicle mapping</summary>
    Task<UserVehicleMapDto> CreateAsync(UserVehicleMapDto dto);
    /// <summary>Update an existing user-vehicle mapping</summary>
    Task<UserVehicleMapDto> UpdateAsync(long id, UserVehicleMapDto dto);
    /// <summary>Delete a user-vehicle mapping</summary>
    Task<bool> DeleteAsync(long id);
}

