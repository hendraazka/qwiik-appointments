using Serilog.Context;
namespace Qwiik.Appointments.Api.Middleware;

public class CorrelationIdMiddleware
{
    public const string Header = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var id = ctx.Request.Headers[Header].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N");
        ctx.Items[Header] = id;
        ctx.Response.Headers[Header] = id;
        using (LogContext.PushProperty("CorrelationId", id))
            await _next(ctx);
    }
}
