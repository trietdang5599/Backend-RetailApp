using ProductManagement.Domain.Enums;

namespace ProductManagement.Application.Products.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ShortDescription,
    decimal BasePrice,
    decimal? SalePrice,
    string? SKU,
    string? Brand,
    Guid CategoryId,
    string? CategoryName,
    ProductStatus Status,
    string StatusLabel,
    int TotalStock,
    string? PrimaryImageUrl,
    List<ProductVariantDto> Variants,
    List<ProductImageDto> Images,
    Dictionary<string, object>? Attributes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ProductSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    decimal BasePrice,
    decimal? SalePrice,
    string? Brand,
    string? CategoryName,
    ProductStatus Status,
    int TotalStock,
    string? PrimaryImageUrl,
    int VariantCount,
    DateTime CreatedAt
);

public record ProductVariantDto(
    Guid Id,
    string? Size,
    string? Color,
    string? ColorHex,
    string? SKU,
    decimal? PriceAdjustment,
    int StockQuantity,
    bool IsLowStock,
    bool IsOutOfStock
);

public record ProductImageDto(
    Guid Id,
    string Url,
    string? AltText,
    bool IsPrimary,
    int SortOrder
);
