using MediatR;
using ProductManagement.Application.Common;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Products.DTOs;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Products.Queries.GetProducts;

public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    ProductStatus? Status = null,
    string? Brand = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? SortBy = null,
    bool SortDesc = false
) : IRequest<PagedResult<ProductSummaryDto>>;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductSummaryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;

    public GetProductsHandler(IUnitOfWork uow, ICacheService cache)
    {
        _uow = uow;
        _cache = cache;
    }

    public async Task<PagedResult<ProductSummaryDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var cacheKey = $"products:{request.Page}:{request.PageSize}:{request.Search}:{request.CategoryId}:{request.Status}:{request.Brand}:{request.MinPrice}:{request.MaxPrice}:{request.SortBy}:{request.SortDesc}";

        var cached = await _cache.GetAsync<PagedResult<ProductSummaryDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _uow.Products.GetPagedAsync(
            page, pageSize, request.Search, request.CategoryId,
            request.Status, request.Brand, request.MinPrice, request.MaxPrice,
            request.SortBy, request.SortDesc, ct);

        var result = new PagedResult<ProductSummaryDto>
        {
            Items = items.Select(p => new ProductSummaryDto(
                p.Id, p.Name, p.Slug, p.BasePrice, p.SalePrice,
                p.Brand, p.Category?.Name,
                p.Status, p.TotalStock,
                p.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                p.Variants.Count, p.CreatedAt)),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
        return result;
    }
}
