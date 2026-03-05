using System;
using System.Collections.Generic;
using System.Security.Claims;

public interface ICurrentUserService
{
    Guid UserId { get; }
    int AccountId { get; }
    int RoleId { get; }
    string Role { get; }
    string HierarchyPath { get; }
    bool IsSystemRole { get; }
    IReadOnlyCollection<int> AccessibleAccountIds { get; }
    bool IsAuthenticated { get; }
    bool IsSystem { get; }
}
