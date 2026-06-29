using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.Categories.Commands;
using ProductManagement.Application.Categories.DTOs;
using ProductManagement.Application.Categories.Queries;

namespace ProductManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] bool tree = true, [FromQuery] bool leafOnly = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(tree, leafOnly), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ValidationErrors.Any())
                return BadRequest(new { errors = result.ValidationErrors });
            return BadRequest(new { error = result.Error });
        }
        return StatusCode(201, result.Value);
    }
}
