using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductManagement.Application.Common;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Products.DTOs;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Products.Commands.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IProductAttributeService _attributeService;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateProductHandler> _logger;

    public UpdateProductHandler(IUnitOfWork uow, IProductAttributeService attributeService,
        ICacheService cache, ILogger<UpdateProductHandler> logger)
    {
        _uow = uow;
        _attributeService = attributeService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(request.Id, ct);
        if (product is null) return Result<ProductDto>.Failure("Product not found");

        var category = await _uow.Categories.GetByIdAsync(request.CategoryId, ct);
        if (category is null) return Result<ProductDto>.Failure("Category not found");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Name)
            : request.Slug;

        if (await _uow.Products.SlugExistsAsync(slug, request.Id, ct))
            return Result<ProductDto>.Failure("Slug already taken by another product");

        if (!string.IsNullOrWhiteSpace(request.SKU) &&
            await _uow.Products.SkuExistsAsync(request.SKU, request.Id, ct))
            return Result<ProductDto>.Failure($"SKU '{request.SKU}' already exists");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // product is already tracked by EF from GetByIdAsync — no need to call Update() again.
            // Change tracking detects all modifications automatically.
            product.Update(request.Name, slug, request.BasePrice, request.CategoryId,
                request.Description, request.ShortDescription, request.SalePrice,
                request.SKU, request.Brand, request.Status);

            // Full-replace variant collection:
            // Step A — mark existing variants Deleted + update product scalars, then SaveChanges.
            // Step B — add new variants via DbSet (no navigation fixup issues), then SaveChanges.
            if (request.Variants is not null)
            {
                _uow.Products.RemoveVariants(product.Variants.ToList());
                product.ClearVariants();
                await _uow.SaveChangesAsync(ct); // DELETE old variants + UPDATE product

                if (request.Variants.Count > 0)
                {
                    var newVariants = request.Variants
                        .Select(v => ProductVariant.Create(
                            product.Id, v.StockQuantity, v.Size, v.Color, v.ColorHex, v.SKU, v.PriceAdjustment))
                        .ToList();
                    await _uow.Products.AddVariantsAsync(newVariants, ct);
                    await _uow.SaveChangesAsync(ct); // INSERT new variants
                    // EF relationship fixup already added newVariants to product.Variants
                }
            }
            else
            {
                await _uow.SaveChangesAsync(ct); // No variant change — just UPDATE product scalars
            }

            // Attributes live in MongoDB — update separately (no EF save needed unless docId changes)
            if (request.Attributes?.Any() == true)
            {
                if (product.AttributeDocumentId is not null)
                {
                    await _attributeService.UpdateAttributesAsync(product.AttributeDocumentId, request.Attributes, ct);
                }
                else
                {
                    var docId = await _attributeService.SaveAttributesAsync(product.Id, request.Attributes, ct);
                    product.SetAttributeDocumentId(docId);
                    await _uow.SaveChangesAsync(ct); // persist the new docId linkback
                }
            }

            await _uow.CommitTransactionAsync(ct);

            await _cache.RemoveByPatternAsync("products:*", ct);
            await _cache.RemoveAsync($"product:{request.Id}", ct);

            _logger.LogInformation("Product {Id} updated", request.Id);

            return Result<ProductDto>.Success(new ProductDto(
                product.Id, product.Name, product.Slug, product.Description,
                product.ShortDescription, product.BasePrice, product.SalePrice,
                product.SKU, product.Brand, product.CategoryId, category.Name,
                product.Status, product.Status.ToString(), product.TotalStock,
                product.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                product.Variants.Select(v => new ProductVariantDto(
                    v.Id, v.Size, v.Color, v.ColorHex, v.SKU,
                    v.PriceAdjustment, v.StockQuantity, v.IsLowStock, v.IsOutOfStock)).ToList(),
                product.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder)).ToList(),
                request.Attributes, product.CreatedAt, product.UpdatedAt));
        }
        catch (OperationCanceledException)
        {
            await _uow.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(CancellationToken.None);
            _logger.LogError(ex, "Failed to update product {Id}", request.Id);
            return Result<ProductDto>.Failure("Failed to update product");
        }
    }

    private static string GenerateSlug(string name) =>
        name.ToLowerInvariant().Replace(" ", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (acc, c) => acc + c).Trim('-');
}
