using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using System.Collections.Generic;

namespace ProductManagement.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? search = null,
        Guid? categoryId = null,
        ProductStatus? status = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken ct = default);
    Task DeleteVariantsByProductIdAsync(Guid productId, CancellationToken ct = default);
    void RemoveVariants(IEnumerable<ProductVariant> variants);
    Task AddVariantsAsync(IEnumerable<ProductVariant> variants, CancellationToken ct = default);
}
