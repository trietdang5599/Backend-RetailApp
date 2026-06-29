using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductManagement.Application.Common;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Products.DTOs;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Products.Commands.CreateProduct;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateProductCommand> _validator;
    private readonly IProductAttributeService _attributeService;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(
        IUnitOfWork uow,
        IValidator<CreateProductCommand> validator,
        IProductAttributeService attributeService,
        ICacheService cache,
        ILogger<CreateProductHandler> logger)
    {
        _uow = uow;
        _validator = validator;
        _attributeService = attributeService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ProductDto>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var categoryExists = await _uow.Categories.GetByIdAsync(request.CategoryId, ct);
        if (categoryExists is null)
            return Result<ProductDto>.Failure("Category not found");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Name)
            : request.Slug;

        if (await _uow.Products.SlugExistsAsync(slug, ct: ct))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..8]}";

        if (!string.IsNullOrWhiteSpace(request.SKU) &&
            await _uow.Products.SkuExistsAsync(request.SKU, ct: ct))
            return Result<ProductDto>.Failure($"SKU '{request.SKU}' already exists");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var product = Product.Create(
                request.Name, slug, request.BasePrice, request.CategoryId,
                request.Description, request.ShortDescription,
                request.SalePrice, request.SKU, request.Brand);

            // Add variants to the graph BEFORE AddAsync — EF resolves insert order via FK
            if (request.Variants?.Any() == true)
            {
                foreach (var v in request.Variants)
                {
                    var variant = ProductVariant.Create(
                        product.Id, v.StockQuantity, v.Size, v.Color, v.ColorHex, v.SKU, v.PriceAdjustment);
                    product.AddVariant(variant);
                }
            }

            // Save MongoDB attributes before first SaveChanges so docId is included
            if (request.Attributes?.Any() == true)
            {
                var docId = await _attributeService.SaveAttributesAsync(product.Id, request.Attributes, ct);
                product.SetAttributeDocumentId(docId);
            }

            // Single AddAsync + single SaveChanges — EF handles Product→Variant insert order
            await _uow.Products.AddAsync(product, ct);
            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            await _cache.RemoveByPatternAsync("products:*", ct);
            _logger.LogInformation("Product {Id} created: {Name}", product.Id, product.Name);

            return Result<ProductDto>.Success(MapToDto(product, categoryExists.Name));
        }
        catch (OperationCanceledException)
        {
            await _uow.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(CancellationToken.None);
            _logger.LogError(ex, "Failed to create product {Name}", request.Name);
            return Result<ProductDto>.Failure("Failed to create product");
        }
    }

    private static string GenerateSlug(string name) =>
        name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("đ", "d")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (acc, c) => acc + c)
            .Trim('-');

    private static ProductDto MapToDto(Product p, string categoryName) => new(
        p.Id, p.Name, p.Slug, p.Description, p.ShortDescription,
        p.BasePrice, p.SalePrice, p.SKU, p.Brand,
        p.CategoryId, categoryName, p.Status, p.Status.ToString(),
        p.TotalStock, p.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
        p.Variants.Select(v => new ProductVariantDto(
            v.Id, v.Size, v.Color, v.ColorHex, v.SKU,
            v.PriceAdjustment, v.StockQuantity, v.IsLowStock, v.IsOutOfStock)).ToList(),
        p.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder)).ToList(),
        null, p.CreatedAt, p.UpdatedAt);
}
