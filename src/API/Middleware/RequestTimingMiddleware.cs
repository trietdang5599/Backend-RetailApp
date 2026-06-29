using System.Diagnostics;

namespace ProductManagement.API.Middleware;

public class RequestTimingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();
        await next(ctx);
        ctx.Response.Headers["X-Response-Time-Ms"] = sw.ElapsedMilliseconds.ToString();
    }
}
