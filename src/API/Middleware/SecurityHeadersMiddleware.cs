namespace ProductManagement.API.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        ctx.Response.Headers.XContentTypeOptions = "nosniff";
        ctx.Response.Headers.XFrameOptions = "DENY";
        ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
        await next(ctx);
    }
}
