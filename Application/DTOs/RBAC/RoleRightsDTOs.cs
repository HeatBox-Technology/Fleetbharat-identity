using System;
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
    public string RoleCode { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public List<RoleFormRightDto> Rights { get; set; } = new();
}

public class UpdateRoleRequest
{
    [Required, MaxLength(100)] public string RoleName { get; set; } = "";
    [MaxLength(200)] public string? Description { get; set; }
    public string RoleCode { get; set; } = "";
    public bool IsActive { get; set; }
}
public class RoleCardSummaryDto
{
    public int TotalRoles { get; set; }
    public int SystemRoles { get; set; }
    public int CustomRoles { get; set; }
}

public class RoleListItemDto
{
    public int RoleId { get; set; }

    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";

    public string RoleName { get; set; } = "";
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public int AssignedUsers { get; set; } // ✅ count

    public DateTime CreatedOn { get; set; }
}

public class RoleListUiResponseDto
{
    public RoleCardSummaryDto Summary { get; set; } = new();
    public PagedResultDto<RoleListItemDto> Roles { get; set; } = new();
}
public class RoleDetailResponseDto
{
    public int RoleId { get; set; }

    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";

    public string RoleName { get; set; } = "";
    public string? Description { get; set; }
    public string RoleCode { get; set; } = "";
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }

    public List<FormRightResponseDto> Rights { get; set; } = new();

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
