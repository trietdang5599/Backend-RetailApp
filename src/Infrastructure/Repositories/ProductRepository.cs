using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.Interfaces;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? search = null,
        Guid? categoryId = null,
        ProductStatus? status = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken ct = default)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .AsSplitQuery()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{search}%") ||
                (p.Brand != null && EF.Functions.ILike(p.Brand, $"%{search}%")) ||
                (p.SKU != null && EF.Functions.ILike(p.SKU, $"%{search}%")));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(p => p.Brand == brand);

        if (minPrice.HasValue)
            query = query.Where(p => p.BasePrice >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.BasePrice <= maxPrice.Value);

        var total = await query.CountAsync(ct);

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("name", false) => query.OrderBy(p => p.Name),
            ("name", true) => query.OrderByDescending(p => p.Name),
            ("price", false) => query.OrderBy(p => p.BasePrice),
            ("price", true) => query.OrderByDescending(p => p.BasePrice),
            ("createdat", false) => query.OrderBy(p => p.CreatedAt),
            ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        await _db.Products.AddAsync(product, ct);
        return product;
    }

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Update(product);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is not null) product.MarkAsDeleted();
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await _db.Products.AnyAsync(p => p.Id == id, ct);

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default) =>
        await _db.Products.AnyAsync(p => p.Slug == slug && (!excludeId.HasValue || p.Id != excludeId.Value), ct);

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken ct = default) =>
        await _db.Products.AnyAsync(p => p.SKU == sku && (!excludeId.HasValue || p.Id != excludeId.Value), ct);

    public async Task DeleteVariantsByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        // Detach tracked variants first so EF doesn't try to DELETE them again on SaveChanges
        var tracked = _db.ChangeTracker.Entries<ProductVariant>()
            .Where(e => e.Entity.ProductId == productId)
            .ToList();
        foreach (var entry in tracked)
            entry.State = EntityState.Detached;

        await _db.Set<ProductVariant>().Where(v => v.ProductId == productId).ExecuteDeleteAsync(ct);
    }

    public void RemoveVariants(IEnumerable<ProductVariant> variants) =>
        _db.RemoveRange(variants);

    public async Task AddVariantsAsync(IEnumerable<ProductVariant> variants, CancellationToken ct = default) =>
        await _db.Set<ProductVariant>().AddRangeAsync(variants, ct);
}
