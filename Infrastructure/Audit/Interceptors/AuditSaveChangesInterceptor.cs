using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuditLoggingOptions _options;
    private readonly string _serviceName;

    public AuditSaveChangesInterceptor(
        IAuditLogger auditLogger,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuditLoggingOptions> options)
    {
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _serviceName = AppDomain.CurrentDomain.FriendlyName;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || eventData.Context == null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(x =>
                x.Entity is not AuditLog &&
                x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var email = GetEmail();
        var user = _httpContextAccessor.HttpContext?.User;
        var accountId = ResolveAccountId(user, _currentUser.AccountId);
        var userId = ResolveUserId(user, _currentUser.UserId);
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var log = new AuditLog
            {
                AccountId = accountId,
                UserId = userId,
                Email = email,
                ServiceName = _serviceName,
                Module = entry.Metadata.ClrType.Name,
                Action = MapOperation(entry.State),
                Endpoint = "EFCore.SaveChanges",
                EventType = "EntityChange",
                Status = 0,
                ExecutionTimeMs = 0,
                CorrelationId = _httpContextAccessor.HttpContext?.TraceIdentifier,
                CreatedAt = now
            };

            var key = GetPrimaryKey(entry);
            if (!string.IsNullOrWhiteSpace(key))
            {
                log.Action = $"{log.Action}:{key}";
            }

            _ = _auditLogger.LogAsync(log, cancellationToken);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static string MapOperation(EntityState state) => state switch
    {
        EntityState.Added => "Create",
        EntityState.Modified => "Update",
        EntityState.Deleted => "Delete",
        _ => "Unknown"
    };

    private static string? GetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        if (key == null)
            return null;

        var value = key.CurrentValue ?? key.OriginalValue;
        return value?.ToString();
    }

    private string? GetEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return null;

        return user.FindFirstValue(ClaimTypes.Email)
               ?? user.FindFirstValue("Email")
               ?? user.FindFirstValue("email");
    }

    private static int? ResolveAccountId(ClaimsPrincipal? user, int fallbackAccountId)
    {
        var claimValue =
            user?.FindFirstValue("AccountId") ??
            user?.FindFirstValue("accountId") ??
            user?.FindFirstValue("account_id") ??
            user?.FindFirstValue("tenant_id");

        if (int.TryParse(claimValue, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return fallbackAccountId > 0 ? fallbackAccountId : null;
    }

    private static Guid? ResolveUserId(ClaimsPrincipal? user, Guid fallbackUserId)
    {
        var claimValue =
            user?.FindFirstValue("UserId") ??
            user?.FindFirstValue("userId") ??
            user?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user?.FindFirstValue("sub");

        if (Guid.TryParse(claimValue, out var parsed))
        {
            return parsed;
        }

        return fallbackUserId != Guid.Empty ? fallbackUserId : null;
    }
}
