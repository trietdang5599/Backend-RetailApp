using FluentAssertions;
using ProductManagement.Domain.Entities;
using Xunit;

namespace ProductManagement.Tests.Domain;

public class ProductVariantTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var variant = ProductVariant.Create(Guid.NewGuid(), 10, size: "M", color: "Red");
        variant.StockQuantity.Should().Be(10);
        variant.IsOutOfStock.Should().BeFalse();
        variant.IsLowStock.Should().BeFalse();
    }

    [Fact]
    public void Create_WithZeroStock_ShouldBeOutOfStock()
    {
        var variant = ProductVariant.Create(Guid.NewGuid(), 0);
        variant.IsOutOfStock.Should().BeTrue();
    }

    [Fact]
    public void AdjustStock_Decrease_ShouldWork()
    {
        var variant = ProductVariant.Create(Guid.NewGuid(), 10);
        variant.AdjustStock(-3);
        variant.StockQuantity.Should().Be(7);
    }

    [Fact]
    public void AdjustStock_BelowZero_ShouldThrow()
    {
        var variant = ProductVariant.Create(Guid.NewGuid(), 5);
        var action = () => variant.AdjustStock(-10);
        action.Should().Throw<InvalidOperationException>().WithMessage("*Insufficient*");
    }

    [Fact]
    public void IsLowStock_WhenBelowThreshold_ShouldBeTrue()
    {
        var variant = ProductVariant.Create(Guid.NewGuid(), 3, lowStockThreshold: 5);
        variant.IsLowStock.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNegativeStock_ShouldThrow()
    {
        var action = () => ProductVariant.Create(Guid.NewGuid(), -1);
        action.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }
}
