using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.Interfaces;

namespace ProductManagement.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(ICacheService cache) : ControllerBase
{
    [HttpDelete("cache")]
    public async Task<IActionResult> ClearCache(CancellationToken ct)
    {
        await cache.RemoveByPatternAsync("products:*", ct);
        await cache.RemoveByPatternAsync("categories:*", ct);
        return Ok(new { message = "Cache cleared successfully" });
    }
}
