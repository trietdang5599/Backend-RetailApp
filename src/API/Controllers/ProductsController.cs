using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductManagement.Application.Common;
using ProductManagement.Application.Products.Commands.CreateProduct;
using ProductManagement.Application.Products.Commands.DeleteProduct;
using ProductManagement.Application.Products.Commands.UpdateProduct;
using ProductManagement.Application.Products.DTOs;
using ProductManagement.Application.Products.Queries.GetProductById;
using ProductManagement.Application.Products.Queries.GetProducts;
using ProductManagement.Domain.Enums;

namespace ProductManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    /// <summary>List products with filtering, sorting and pagination</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryDto>), 200)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] ProductStatus? status = null,
        [FromQuery] string? brand = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            return BadRequest(new { error = "minPrice cannot exceed maxPrice" });

        var result = await _mediator.Send(
            new GetProductsQuery(page, pageSize, search, categoryId, status, brand, minPrice, maxPrice, sortBy, sortDesc), ct);
        return Ok(result);
    }

    /// <summary>Get product by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Create a new product</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ValidationErrors.Any())
                return BadRequest(new { errors = result.ValidationErrors });
            return BadRequest(new { error = result.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Update an existing product</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var command = new UpdateProductCommand(
            id, request.Name, request.Slug, request.BasePrice, request.CategoryId,
            request.Description, request.ShortDescription, request.SalePrice,
            request.SKU, request.Brand, request.Status, request.Attributes, request.Variants);

        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ValidationErrors.Any())
                return BadRequest(new { errors = result.ValidationErrors });
            return result.Error == "Product not found" ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>Soft delete a product</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    /// <summary>Patch product status</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> PatchStatus(Guid id, [FromBody] PatchStatusRequest request, CancellationToken ct)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (!product.IsSuccess) return NotFound(new { error = product.Error });

        var p = product.Value!;
        var command = new UpdateProductCommand(id, p.Name, p.Slug, p.BasePrice, p.CategoryId,
            p.Description, p.ShortDescription, p.SalePrice, p.SKU, p.Brand, request.Status, null, null);

        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}

public record UpdateProductRequest(
    string Name,
    string? Slug,
    decimal BasePrice,
    Guid CategoryId,
    string? Description,
    string? ShortDescription,
    decimal? SalePrice,
    string? SKU,
    string? Brand,
    ProductStatus Status,
    Dictionary<string, object>? Attributes,
    List<VariantInput>? Variants
);

public record PatchStatusRequest(ProductStatus Status);
