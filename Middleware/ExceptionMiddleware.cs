using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;


    public ExceptionMiddleware(RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }



    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // 🔥 LOG REAL ERROR
            _logger.LogError(ex, "Unhandled Exception");

            var dbLogger = context.RequestServices.GetService<DbLogger>();
            if (dbLogger != null)
            {
                try
                {
                    await dbLogger.LogAsync(ex, context);
                }
                catch
                {
                    // never break API if logging fails
                }
            }
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        int statusCode = StatusCodes.Status500InternalServerError;

        if (ex is BadHttpRequestException)
            statusCode = 400;
        else if (ex is UnauthorizedAccessException)
            statusCode = 401;
        else if (ex is KeyNotFoundException)
            statusCode = 404;
        else if (ex is InvalidOperationException)
            statusCode = 409;

        context.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.Fail(
            ex.Message,   // ✅ send real message
            statusCode);

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}