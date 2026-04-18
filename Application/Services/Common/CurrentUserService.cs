using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    private readonly BackgroundCurrentUserContext _backgroundContext;

    public CurrentUserService(IHttpContextAccessor http, BackgroundCurrentUserContext backgroundContext)
    {
        _http = http;
        _backgroundContext = backgroundContext;
    }

    private ClaimsPrincipal? User => _http.HttpContext?.User;
    private bool HasBackgroundContext =>
        _backgroundContext.IsAuthenticated ||
        _backgroundContext.AccountId > 0 ||
        _backgroundContext.AccessibleAccountIds.Count > 0;

    public Guid UserId =>
        HasBackgroundContext
            ? _backgroundContext.UserId
            :
        Guid.TryParse(
            User?.FindFirstValue("UserId") ??
            User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User?.FindFirstValue("sub"),
            out var userId)
            ? userId
            : Guid.Empty;

    public int AccountId =>
        HasBackgroundContext
            ? _backgroundContext.AccountId
            :
        int.TryParse(
            User?.FindFirstValue("AccountId") ??
            User?.FindFirstValue("accountId"),
            out var id)
            ? id : 0;

    public int RoleId =>
        HasBackgroundContext
            ? _backgroundContext.RoleId
            :
        int.TryParse(
            User?.FindFirstValue("RoleId") ??
            User?.FindFirstValue("roleId"),
            out var roleId)
            ? roleId : 0;

    public string Role =>
        HasBackgroundContext
            ? _backgroundContext.Role
            :
        User?.FindFirstValue(ClaimTypes.Role) ??
        User?.FindFirstValue("Role") ??
        "";

    public string HierarchyPath =>
        HasBackgroundContext
            ? _backgroundContext.HierarchyPath
            :
        User?.FindFirstValue("HierarchyPath") ??
        User?.FindFirstValue("hierarchyPath") ??
        "";

    public bool IsAuthenticated =>
        HasBackgroundContext
            ? _backgroundContext.IsAuthenticated
            :
        User?.Identity?.IsAuthenticated == true;

    public bool IsSystemRole =>
        HasBackgroundContext
            ? _backgroundContext.IsSystemRole
            :
        bool.TryParse(User?.FindFirstValue("IsSystemRole"), out var isSystemRole)
            ? isSystemRole
            : false;

    public IReadOnlyCollection<int> AccessibleAccountIds
    {
        get
        {
            if (HasBackgroundContext)
                return _backgroundContext.AccessibleAccountIds;

            var raw = User?.FindFirstValue("AccessibleAccountIds");

            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<int>();

            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToArray();
        }
    }

    public bool IsSystem =>
        IsSystemRole ||
        Role.Equals("System", StringComparison.OrdinalIgnoreCase);
}
