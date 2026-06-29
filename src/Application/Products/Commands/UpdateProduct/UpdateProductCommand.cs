using MediatR;
using ProductManagement.Application.Common;
using ProductManagement.Application.Products.DTOs;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Application.Products.Commands.UpdateProduct;

public record VariantInput(
    string? Size,
    string? Color,
    string? ColorHex,
    string? SKU,
    int StockQuantity,
    decimal? PriceAdjustment
);

public record UpdateProductCommand(
    Guid Id,
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
) : IRequest<Result<ProductDto>>;
