using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db) => _db = db;

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Categories.Include(c => c.Children).Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Categories.Include(c => c.Children).Include(c => c.Products)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken ct = default) =>
        await _db.Categories.Where(c => c.ParentId == null)
            .Include(c => c.Children).AsNoTracking().ToListAsync(ct);

    public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
    {
        await _db.Categories.AddAsync(category, ct);
        return category;
    }

    public Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Update(category);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var cat = await _db.Categories.FindAsync(new object[] { id }, ct);
        if (cat is not null) cat.MarkAsDeleted();
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default) =>
        await _db.Categories.AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value), ct);

    public async Task<bool> HasProductsAsync(Guid id, CancellationToken ct = default) =>
        await _db.Products.AnyAsync(p => p.CategoryId == id, ct);
}
