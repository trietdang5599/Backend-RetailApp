using MediatR;
using ProductManagement.Application.Common;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Products.DTOs;
using ProductManagement.Domain.Interfaces;
using System.Text.Json;

namespace ProductManagement.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IProductAttributeService _attributeService;
    private readonly ICacheService _cache;

    public GetProductByIdHandler(IUnitOfWork uow, IProductAttributeService attributeService, ICacheService cache)
    {
        _uow = uow;
        _attributeService = attributeService;
        _cache = cache;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var cacheKey = $"product:{request.Id}";
        var cached = await _cache.GetAsync<ProductDto>(cacheKey, ct);
        if (cached is not null) return Result<ProductDto>.Success(cached);

        var product = await _uow.Products.GetByIdAsync(request.Id, ct);
        if (product is null) return Result<ProductDto>.Failure("Product not found");

        Dictionary<string, object>? attributes = null;
        if (product.AttributeDocumentId is not null)
        {
            var json = await _attributeService.GetAttributesAsync(product.AttributeDocumentId, ct);
            if (json is not null)
                attributes = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }

        var dto = new ProductDto(
            product.Id, product.Name, product.Slug, product.Description,
            product.ShortDescription, product.BasePrice, product.SalePrice,
            product.SKU, product.Brand, product.CategoryId, product.Category?.Name,
            product.Status, product.Status.ToString(), product.TotalStock,
            product.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
            product.Variants.Select(v => new ProductVariantDto(
                v.Id, v.Size, v.Color, v.ColorHex, v.SKU,
                v.PriceAdjustment, v.StockQuantity, v.IsLowStock, v.IsOutOfStock)).ToList(),
            product.Images.OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder)).ToList(),
            attributes, product.CreatedAt, product.UpdatedAt);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), ct);
        return Result<ProductDto>.Success(dto);
    }
}
