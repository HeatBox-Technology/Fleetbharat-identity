using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http)
    {
        _http = http;
    }

    private ClaimsPrincipal User => _http.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(
            User?.FindFirstValue("UserId") ??
            User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User?.FindFirstValue("sub"),
            out var userId)
            ? userId
            : Guid.Empty;

    public int AccountId =>
        int.TryParse(
            User?.FindFirstValue("AccountId") ??
            User?.FindFirstValue("accountId"),
            out var id)
            ? id : 0;

    public int RoleId =>
        int.TryParse(
            User?.FindFirstValue("RoleId") ??
            User?.FindFirstValue("roleId"),
            out var roleId)
            ? roleId : 0;

    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role) ??
        User?.FindFirstValue("Role") ??
        "";

    public string HierarchyPath =>
        User?.FindFirstValue("HierarchyPath") ??
        User?.FindFirstValue("hierarchyPath") ??
        "";

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public bool IsSystemRole =>
        bool.TryParse(User?.FindFirstValue("IsSystemRole"), out var isSystemRole)
            ? isSystemRole
            : false;

    public IReadOnlyCollection<int> AccessibleAccountIds
    {
        get
        {
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
