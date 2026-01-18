using System;

public class mst_form
{
    public int FormId { get; set; }

    public string FormCode { get; set; }           // USER_MGMT

    public string FormName { get; set; }           // User Management

    public string ModuleName { get; set; }         // Admin / Fleet / Billing

    public string PageUrl { get; set; }            // /admin/users

    public string PageComponent { get; set; }      // users/page.tsx

    public string IconName { get; set; }            // lucide icon name

    public int? SortOrder { get; set; }

    public bool IsMenu { get; set; }

    public bool IsActive { get; set; }

    public int? ParentFormId { get; set; }          // Self reference
    public string pageType { get; set; }          // Menu / SubMenu / Button
    public string PermissionType { get; set; }   // Read / Write / Delete
    public enum PageUseType
    {
        Mobile,
        Web
    }
    public PageUseType UseType { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public DateTime? DeletedOn { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public int? DeletedBy { get; set; }
}
