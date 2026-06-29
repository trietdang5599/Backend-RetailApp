using FluentValidation;

namespace ProductManagement.Application.Products.Commands.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base price must be non-negative");

        RuleFor(x => x.SalePrice)
            .LessThanOrEqualTo(x => x.BasePrice)
            .When(x => x.SalePrice.HasValue)
            .WithMessage("Sale price cannot exceed base price");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

        RuleFor(x => x.SKU)
            .MaximumLength(100).When(x => x.SKU != null)
            .WithMessage("SKU must not exceed 100 characters");

        RuleFor(x => x.Brand)
            .MaximumLength(100).When(x => x.Brand != null)
            .WithMessage("Brand must not exceed 100 characters");

        RuleForEach(x => x.Variants)
            .ChildRules(v =>
            {
                v.RuleFor(x => x.StockQuantity)
                    .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative");
            });
    }
}
