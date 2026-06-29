using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Products.Commands.CreateProduct;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;
using Xunit;

namespace ProductManagement.Tests.Application;

public class CreateProductHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<IProductAttributeService> _attributeService = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<ILogger<CreateProductHandler>> _logger = new();

    public CreateProductHandlerTests()
    {
        _uow.Setup(u => u.Products).Returns(_productRepo.Object);
        _uow.Setup(u => u.Categories).Returns(_categoryRepo.Object);
        _cache.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CreateProductHandler CreateHandler() => new(
        _uow.Object,
        new CreateProductValidator(),
        _attributeService.Object,
        _cache.Object,
        _logger.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateProduct()
    {
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Shirts", "shirts");

        _categoryRepo.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _productRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productRepo.Setup(r => r.SkuExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productRepo.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateProductCommand("Test Shirt", null, 199000, categoryId,
            "A great shirt", null, null, "SHIRT-001", "Nike", null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Shirt");
        result.Value.SKU.Should().Be("SHIRT-001");
    }

    [Fact]
    public async Task Handle_InvalidPrice_ShouldReturnValidationFailure()
    {
        var command = new CreateProductCommand("Shirt", null, -100, Guid.NewGuid(),
            null, null, null, null, null, null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ShouldFail()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var command = new CreateProductCommand("Shirt", null, 100, Guid.NewGuid(),
            null, null, null, null, null, null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Category not found");
    }

    [Fact]
    public async Task Handle_DuplicateSku_ShouldFail()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Category.Create("Shirts", "shirts"));
        _productRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productRepo.Setup(r => r.SkuExistsAsync("SHIRT-001", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateProductCommand("Shirt", null, 100, categoryId,
            null, null, null, "SHIRT-001", null, null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("SKU");
    }
}
