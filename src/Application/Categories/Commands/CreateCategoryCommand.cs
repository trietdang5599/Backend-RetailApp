using FluentValidation;
using MediatR;
using ProductManagement.Application.Categories.DTOs;
using ProductManagement.Application.Common;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Categories.Commands;

public record CreateCategoryCommand(
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentId
) : IRequest<Result<CategoryDto>>;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        RuleFor(x => x.ParentId).NotNull().WithMessage("Must select a parent category (Tops, Bottoms, or Accessories)");
    }
}

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateCategoryCommand> _validator;

    public CreateCategoryHandler(IUnitOfWork uow, IValidator<CreateCategoryCommand> validator)
    {
        _uow = uow;
        _validator = validator;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<CategoryDto>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var parent = await _uow.Categories.GetByIdAsync(request.ParentId!.Value, ct);
        if (parent is null)
            return Result<CategoryDto>.Failure("Parent category not found");
        if (parent.ParentId.HasValue)
            return Result<CategoryDto>.Failure("Can only create sub-categories under top-level categories (Tops, Bottoms, Accessories)");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Name.ToLowerInvariant().Replace(" ", "-").Where(c => char.IsLetterOrDigit(c) || c == '-').Aggregate("", (a, c) => a + c)
            : request.Slug;

        if (await _uow.Categories.SlugExistsAsync(slug, ct: ct))
            return Result<CategoryDto>.Failure("Category slug already exists");

        var category = Category.Create(request.Name, slug, request.Description, request.ParentId);
        await _uow.Categories.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CategoryDto>.Success(new CategoryDto(
            category.Id, category.Name, category.Slug, category.Description,
            category.ParentId, null, 0, new List<CategoryDto>()));
    }
}
