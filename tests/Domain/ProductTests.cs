using FluentAssertions;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using Xunit;

namespace ProductManagement.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var categoryId = Guid.NewGuid();
        var product = Product.Create("Test Shirt", "test-shirt", 199000, categoryId, brand: "Nike");

        product.Name.Should().Be("Test Shirt");
        product.Slug.Should().Be("test-shirt");
        product.BasePrice.Should().Be(199000);
        product.Status.Should().Be(ProductStatus.Draft);
        product.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldThrow()
    {
        var action = () => Product.Create("Shirt", "shirt", -100, Guid.NewGuid());
        action.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithSalePriceExceedingBasePrice_ShouldThrow()
    {
        var action = () => Product.Create("Shirt", "shirt", 100, Guid.NewGuid(), salePrice: 200);
        action.Should().Throw<ArgumentException>().WithMessage("*Sale price*");
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetIsDeleted()
    {
        var product = Product.Create("Shirt", "shirt", 100, Guid.NewGuid());
        product.MarkAsDeleted();
        product.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        var product = Product.Create("Shirt", "shirt", 100, Guid.NewGuid());
        product.Activate();
        product.Status.Should().Be(ProductStatus.Active);
    }

    [Fact]
    public void Update_WithSalePriceExceedingBasePrice_ShouldThrow()
    {
        var product = Product.Create("Shirt", "shirt", 100, Guid.NewGuid());
        var action = () => product.Update("Shirt", "shirt", 100, Guid.NewGuid(),
            null, null, 200, null, null, ProductStatus.Active);
        action.Should().Throw<ArgumentException>();
    }
}
