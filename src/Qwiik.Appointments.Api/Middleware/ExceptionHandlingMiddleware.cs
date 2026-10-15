using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Qwiik.Appointments.Core;
namespace Qwiik.Appointments.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        => (_next, _logger) = (next, logger);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning("Business rule violation: {Message}", ex.Message);
            await Write(ctx, StatusCodes.Status400BadRequest, "Business rule violation", ex.Message);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogWarning(ex, "Unique constraint violation, likely a concurrent booking");
            await Write(ctx, StatusCodes.Status409Conflict, "Conflict",
                "The slot was taken by another request. Please retry.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await Write(ctx, StatusCodes.Status500InternalServerError, "Internal server error",
                "An unexpected error occurred. Quote the correlation ID when reporting it.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException s && s.SqliteErrorCode == 19 && s.Message.Contains("UNIQUE");

    private static Task Write(HttpContext ctx, int status, string title, string detail)
    {
        ctx.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["correlationId"] = ctx.Items[CorrelationIdMiddleware.Header]?.ToString();
        return ctx.Response.WriteAsJsonAsync(problem);
    }
}
