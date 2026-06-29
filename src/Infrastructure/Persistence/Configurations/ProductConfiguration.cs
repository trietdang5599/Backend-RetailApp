using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(250);
        builder.HasIndex(p => p.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Property(p => p.SKU).HasMaxLength(100);
        builder.HasIndex(p => p.SKU).IsUnique().HasFilter("\"SKU\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.Property(p => p.Brand).HasMaxLength(100);
        builder.Property(p => p.BasePrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).HasConversion<int>().HasDefaultValue(ProductStatus.Draft);
        builder.Property(p => p.AttributeDocumentId).HasMaxLength(50);

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.CategoryId, p.Status });
        builder.HasIndex(p => p.Brand);
        builder.HasIndex(p => p.BasePrice);
        builder.HasIndex(p => p.CreatedAt);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Size).HasMaxLength(50);
        builder.Property(v => v.Color).HasMaxLength(50);
        builder.Property(v => v.ColorHex).HasMaxLength(10);
        builder.Property(v => v.SKU).HasMaxLength(100);
        builder.Property(v => v.PriceAdjustment).HasColumnType("decimal(18,2)");
        builder.HasIndex(v => new { v.ProductId, v.Size, v.Color }).IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Must match Product's global query filter to avoid unexpected filtered-out results
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.AltText).HasMaxLength(200);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(150);
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
