using MediatR;
using ProductManagement.Application.Categories.DTOs;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Categories.Queries;

// LeafOnly=true returns only categories with no children — used for product category picker
public record GetCategoriesQuery(bool TreeFormat = true, bool LeafOnly = false) : IRequest<IEnumerable<CategoryDto>>;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    private readonly IUnitOfWork _uow;

    public GetCategoriesHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var all = (await _uow.Categories.GetAllAsync(ct)).ToList();

        if (request.LeafOnly)
            return all
                .Where(c => !all.Any(x => x.ParentId == c.Id))
                .Select(c => new CategoryDto(
                    c.Id, c.Name, c.Slug, c.Description, c.ParentId,
                    all.FirstOrDefault(x => x.Id == c.ParentId)?.Name,
                    c.Products.Count, new()));

        if (!request.TreeFormat)
            return all.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ParentId, null, 0, new()));

        return BuildTree(all, null);
    }

    private static List<CategoryDto> BuildTree(List<Domain.Entities.Category> all, Guid? parentId)
    {
        return all
            .Where(c => c.ParentId == parentId)
            .Select(c =>
            {
                var children = BuildTree(all, c.Id);
                var totalProducts = c.Products.Count + children.Sum(ch => ch.ProductCount);
                return new CategoryDto(
                    c.Id, c.Name, c.Slug, c.Description, c.ParentId,
                    all.FirstOrDefault(x => x.Id == c.ParentId)?.Name,
                    totalProducts,
                    children);
            })
            .ToList();
    }
}
