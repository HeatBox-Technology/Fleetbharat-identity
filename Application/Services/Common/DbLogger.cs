using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class DbLogger
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DbLogger> _logger;

    public DbLogger(IServiceScopeFactory scopeFactory, ILogger<DbLogger> logger)
    {

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogAsync(Exception ex, HttpContext context)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            var log = new ErrorLog
            {
                message = ex.Message ?? "Unknown error",
                stack_trace = ex.StackTrace ?? string.Empty,
                inner_exception = ex.InnerException?.ToString() ?? string.Empty,
                path = context?.Request?.Path.Value ?? string.Empty,
                method = context?.Request?.Method ?? string.Empty,
                created_at = DateTime.UtcNow
            };

            db.ErrorLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            // Logging failure should never break the API response path.
            _logger.LogError(dbEx, "Failed to write error log to database");
        }
    }
}
