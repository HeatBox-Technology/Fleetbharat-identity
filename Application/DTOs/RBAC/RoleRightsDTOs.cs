using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class RoleFormRightDto
{
    public int FormId { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanAll { get; set; }
}

public class CreateRoleRequest
{
    [Required] public int AccountId { get; set; }
    [Required, MaxLength(100)] public string RoleName { get; set; } = "";
    [MaxLength(200)] public string? Description { get; set; }

    public List<RoleFormRightDto> Rights { get; set; } = new();
}

public class UpdateRoleRequest
{
    [Required, MaxLength(100)] public string RoleName { get; set; } = "";
    [MaxLength(200)] public string? Description { get; set; }
    public bool IsActive { get; set; }
}
