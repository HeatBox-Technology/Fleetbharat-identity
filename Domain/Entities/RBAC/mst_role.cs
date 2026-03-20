using System;


public class mst_role : IAccountEntity
{
    public int RoleId { get; set; }
    public int AccountId { get; set; }
    public string RoleCode { get; set; }
    public string RoleName { get; set; }
    public string? Description { get; set; }
    public int? FkCategoryId { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public DateTime? DeletedOn { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public int? DeletedBy { get; set; }

}
