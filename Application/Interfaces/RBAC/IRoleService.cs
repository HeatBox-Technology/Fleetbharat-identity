using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRoleService
{
    Task<int> CreateAsync(CreateRoleRequest req);
    Task<bool> UpdateAsync(int roleId, UpdateRoleRequest req);
    Task<bool> DeleteAsync(int roleId);

    Task<List<mst_role>> GetByAccountAsync(int accountId);

    Task<bool> UpdateRightsAsync(int roleId, List<RoleFormRightDto> rights);

    Task<List<FormRightResponseDto>> GetRoleRightsAsync(int roleId);
    Task<RoleListUiResponseDto> GetRoles(
           int page,
           int pageSize,
           int? accountId,
           string? search);
    Task<byte[]> ExportRolesCsvAsync(int? accountId, string? search);
}
