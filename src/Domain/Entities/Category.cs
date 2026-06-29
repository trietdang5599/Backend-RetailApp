using ProductManagement.Domain.Common;

namespace ProductManagement.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ParentId { get; private set; }
    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { }

    public static Category Create(string name, string slug, string? description = null, Guid? parentId = null)
    {
        return new Category
        {
            Name = name,
            Slug = slug,
            Description = description,
            ParentId = parentId
        };
    }

    public void Update(string name, string slug, string? description, Guid? parentId)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ParentId = parentId;
        UpdateTimestamp();
    }
}
