using ProductManagement.Domain.Common;

namespace ProductManagement.Domain.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }

    private ProductImage() { }

    public static ProductImage Create(Guid productId, string url, string? altText = null,
        bool isPrimary = false, int sortOrder = 0)
    {
        return new ProductImage
        {
            ProductId = productId,
            Url = url,
            AltText = altText,
            IsPrimary = isPrimary,
            SortOrder = sortOrder
        };
    }

    public void SetAsPrimary() => IsPrimary = true;
    public void UnsetPrimary() => IsPrimary = false;
}
