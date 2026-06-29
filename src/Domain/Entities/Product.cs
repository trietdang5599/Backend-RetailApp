using ProductManagement.Domain.Common;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ShortDescription { get; private set; }
    public decimal BasePrice { get; private set; }
    public decimal? SalePrice { get; private set; }
    public string? SKU { get; private set; }
    public string? Brand { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;
    public int SortOrder { get; private set; } = 0;
    // MongoDB document ID for flexible attributes
    public string? AttributeDocumentId { get; private set; }

    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

    private Product() { }

    public static Product Create(
        string name, string slug, decimal basePrice, Guid categoryId,
        string? description = null, string? shortDescription = null,
        decimal? salePrice = null, string? sku = null, string? brand = null)
    {
        if (basePrice < 0) throw new ArgumentException("Base price cannot be negative");
        if (salePrice.HasValue && salePrice.Value > basePrice)
            throw new ArgumentException("Sale price cannot exceed base price");

        return new Product
        {
            Name = name,
            Slug = slug,
            Description = description,
            ShortDescription = shortDescription,
            BasePrice = basePrice,
            SalePrice = salePrice,
            SKU = sku,
            Brand = brand,
            CategoryId = categoryId
        };
    }

    public void Update(string name, string slug, decimal basePrice, Guid categoryId,
        string? description, string? shortDescription, decimal? salePrice,
        string? sku, string? brand, ProductStatus status)
    {
        if (basePrice < 0) throw new ArgumentException("Base price cannot be negative");
        if (salePrice.HasValue && salePrice.Value > basePrice)
            throw new ArgumentException("Sale price cannot exceed base price");

        Name = name;
        Slug = slug;
        Description = description;
        ShortDescription = shortDescription;
        BasePrice = basePrice;
        SalePrice = salePrice;
        SKU = sku;
        Brand = brand;
        CategoryId = categoryId;
        Status = status;
        UpdateTimestamp();
    }

    public void AddVariant(ProductVariant variant) => Variants.Add(variant);
    public void ClearVariants() => Variants.Clear();
    public void AddImage(ProductImage image) => Images.Add(image);
    public void SetAttributeDocumentId(string docId) => AttributeDocumentId = docId;
    public void Activate() { Status = ProductStatus.Active; UpdateTimestamp(); }
    public void Deactivate() { Status = ProductStatus.Inactive; UpdateTimestamp(); }

    public int TotalStock => Variants.Sum(v => v.StockQuantity);
}
