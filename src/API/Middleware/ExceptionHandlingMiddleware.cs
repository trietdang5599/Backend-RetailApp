using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace ProductManagement.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            // client disconnected — no response needed
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            if (!ctx.Response.HasStarted)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new { error = "Internal server error" });
            }
        }
    }
}
