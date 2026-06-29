using ProductManagement.Domain.Common;

namespace ProductManagement.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string? Size { get; private set; }
    public string? Color { get; private set; }
    public string? ColorHex { get; private set; }
    public string? SKU { get; private set; }
    public decimal? PriceAdjustment { get; private set; }
    public int StockQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;

    private ProductVariant() { }

    public static ProductVariant Create(Guid productId, int stockQuantity,
        string? size = null, string? color = null, string? colorHex = null,
        string? sku = null, decimal? priceAdjustment = null, int lowStockThreshold = 5)
    {
        if (stockQuantity < 0) throw new ArgumentException("Stock cannot be negative");
        return new ProductVariant
        {
            ProductId = productId,
            Size = size,
            Color = color,
            ColorHex = colorHex,
            SKU = sku,
            PriceAdjustment = priceAdjustment,
            StockQuantity = stockQuantity,
            LowStockThreshold = lowStockThreshold
        };
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0) throw new ArgumentException("Stock cannot be negative");
        StockQuantity = quantity;
        UpdateTimestamp();
    }

    public void AdjustStock(int delta)
    {
        var newQty = StockQuantity + delta;
        if (newQty < 0) throw new InvalidOperationException("Insufficient stock");
        StockQuantity = newQty;
        UpdateTimestamp();
    }

    public bool IsLowStock => StockQuantity <= LowStockThreshold && StockQuantity > 0;
    public bool IsOutOfStock => StockQuantity == 0;
}
