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
    private readonly ICurrentUserService _currentUser;
    private readonly string _serviceName;

    public AuditMiddleware(
        RequestDelegate next,
        IAuditLogger auditLogger,
        ICurrentUserService currentUser,
        IOptions<AuditLoggingOptions> options)
    {
        _next = next;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _options = options.Value;
        _serviceName = AppDomain.CurrentDomain.FriendlyName;
    }

    public async Task Invoke(HttpContext context)
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

            var log = new AuditLog
            {
                AccountId = _currentUser.AccountId > 0 ? _currentUser.AccountId : null,
                UserId = _currentUser.UserId != Guid.Empty ? _currentUser.UserId : null,
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
