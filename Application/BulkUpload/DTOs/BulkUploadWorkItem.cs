using System.Collections.Generic;
using System;

public class BulkUploadWorkItem
{
    public int JobId { get; set; }
    public string ModuleKey { get; set; } = "";
    public List<Dictionary<string, string>> Rows { get; set; } = new();
    public int? CreatedBy { get; set; }
    public Guid UserId { get; set; }
    public int AccountId { get; set; }
    public int RoleId { get; set; }
    public string Role { get; set; } = "";
    public string HierarchyPath { get; set; } = "";
    public bool IsSystemRole { get; set; }
    public bool IsAuthenticated { get; set; }
    public List<int> AccessibleAccountIds { get; set; } = new();
}
