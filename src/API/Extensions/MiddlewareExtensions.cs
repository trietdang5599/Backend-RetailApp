using ProductManagement.API.Middleware;

namespace ProductManagement.API.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseAppMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestTimingMiddleware>();
        return app;
    }
}
