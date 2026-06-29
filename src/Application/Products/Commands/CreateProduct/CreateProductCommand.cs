using MediatR;
using ProductManagement.Application.Common;
using ProductManagement.Application.Products.DTOs;

namespace ProductManagement.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Slug,
    decimal BasePrice,
    Guid CategoryId,
    string? Description,
    string? ShortDescription,
    decimal? SalePrice,
    string? SKU,
    string? Brand,
    List<CreateVariantDto>? Variants,
    Dictionary<string, object>? Attributes
) : IRequest<Result<ProductDto>>;

public record CreateVariantDto(
    string? Size,
    string? Color,
    string? ColorHex,
    string? SKU,
    decimal? PriceAdjustment,
    int StockQuantity
);
