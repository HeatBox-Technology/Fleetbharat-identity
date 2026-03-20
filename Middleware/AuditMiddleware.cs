using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

public class AuditMiddleware
{
    private static readonly string[] SkipPrefixes = new[]
    {
        "/health",
        "/metrics",
        "/swagger"
    };

    private readonly RequestDelegate _next;
    private readonly IAuditLogger _auditLogger;
    private readonly AuditLoggingOptions _options;
    private readonly string _serviceName;

    public AuditMiddleware(
        RequestDelegate next,
        IAuditLogger auditLogger,
        IOptions<AuditLoggingOptions> options)
    {
        _next = next;
        _auditLogger = auditLogger;
        _options = options.Value;
        _serviceName = AppDomain.CurrentDomain.FriendlyName;
    }

    public async Task Invoke(HttpContext context, ICurrentUserService currentUser)
    {
        if (!_options.Enabled || ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var accountId = ResolveAccountId(context.User, currentUser.AccountId);
            var userId = ResolveUserId(context.User, currentUser.UserId);

            var log = new AuditLog
            {
                AccountId = accountId,
                UserId = userId,
                Email = GetEmail(context),
                ServiceName = _serviceName,
                Module = context.GetRouteValue("controller")?.ToString(),
                Action = context.GetRouteValue("action")?.ToString() ?? context.Request.Method,
                Endpoint = context.Request.Path.Value ?? "",
                EventType = "ApiRequest",
                Status = context.Response.StatusCode,
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                CorrelationId = context.TraceIdentifier,
                CreatedAt = DateTime.UtcNow
            };

            _ = _auditLogger.LogAsync(log, context.RequestAborted);
        }
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

    private bool ShouldSkip(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (string.IsNullOrWhiteSpace(path))
            return true;

        foreach (var prefix in SkipPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (!_options.LogReadRequests && HttpMethods.IsGet(context.Request.Method))
            return true;

        if (path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase))
            return true;

        var extension = System.IO.Path.GetExtension(path);
        if (!string.IsNullOrWhiteSpace(extension))
            return true;

        return false;
    }

    private static string? GetEmail(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("Email")
            ?? user.FindFirstValue("email");
    }
}
