using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISimService
{
    /// <summary>
    /// Get SIMs with dashboard summary + pagination.
    /// </summary>
    Task<SimListUiResponseDto> GetSims(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null);

    /// <summary>
    /// Get paged list only.
    /// </summary>
    Task<PagedResultDto<SimDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null);

    /// <summary>
    /// Get all SIMs.
    /// </summary>
    Task<IEnumerable<SimDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);

    /// <summary>
    /// Get SIM by Id.
    /// </summary>
    Task<SimDto?> GetByIdAsync(int id);

    /// <summary>
    /// Create new SIM.
    /// </summary>
    Task<int> CreateAsync(CreateSimDto dto);

    /// <summary>
    /// Update SIM.
    /// </summary>
    Task<bool> UpdateAsync(int id, UpdateSimDto dto);

    /// <summary>
    /// Update SIM status only.
    /// </summary>
    Task<bool> UpdateStatusAsync(int id, bool isActive);

    /// <summary>
    /// Soft delete SIM.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Bulk create SIMs.
    /// </summary>
    Task<List<SimDto>> BulkCreateAsync(List<CreateSimDto> sims);

    /// <summary>
    /// Export SIMs as CSV with readable column names.
    /// </summary>
    Task<byte[]> ExportSimsCsvAsync(int? accountId = null, string? search = null);
}
