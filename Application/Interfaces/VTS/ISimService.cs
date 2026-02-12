using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


public interface ISimService
{
    /// <summary>
    /// Gets a paged list of SIMs.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>Paged result of SIMs.</returns>
    Task<PagedResultDto<SimDto>> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<SimDto>> GetAllAsync();
    Task<SimDto?> GetByIdAsync(long id);
    Task<SimDto> CreateAsync(SimDto dto);
    Task<SimDto> UpdateAsync(long id, SimDto dto);
    Task<bool> DeleteAsync(long id);
    /// <summary>
    /// Bulk create SIMs.
    /// </summary>
    /// <param name="sims">List of SIMs to create.</param>
    /// <returns>List of created SIMs.</returns>
    Task<IEnumerable<SimDto>> BulkCreateAsync(IEnumerable<SimDto> sims);
}

