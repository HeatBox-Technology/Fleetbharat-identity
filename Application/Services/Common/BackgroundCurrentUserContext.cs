using System;
using System.Collections.Generic;

public class BackgroundCurrentUserContext
{
    public Guid UserId { get; set; }
    public int AccountId { get; set; }
    public int RoleId { get; set; }
    public string Role { get; set; } = "";
    public string HierarchyPath { get; set; } = "";
    public bool IsSystemRole { get; set; }
    public bool IsAuthenticated { get; set; }
    public IReadOnlyCollection<int> AccessibleAccountIds { get; set; } = Array.Empty<int>();

    public void Clear()
    {
        UserId = Guid.Empty;
        AccountId = 0;
        RoleId = 0;
        Role = "";
        HierarchyPath = "";
        IsSystemRole = false;
        IsAuthenticated = false;
        AccessibleAccountIds = Array.Empty<int>();
    }
}
