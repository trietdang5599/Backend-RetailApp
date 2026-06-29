using MediatR;
using ProductManagement.Application.Categories.DTOs;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Categories.Queries;

public record GetCategoriesQuery(bool TreeFormat = true) : IRequest<IEnumerable<CategoryDto>>;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    private readonly IUnitOfWork _uow;

    public GetCategoriesHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var all = (await _uow.Categories.GetAllAsync(ct)).ToList();

        if (!request.TreeFormat)
            return all.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ParentId, null, 0, new()));

        return BuildTree(all, null);
    }

    private static List<CategoryDto> BuildTree(
        List<Domain.Entities.Category> all,
        Guid? parentId)
    {
        return all
            .Where(c => c.ParentId == parentId)
            .Select(c => new CategoryDto(
                c.Id, c.Name, c.Slug, c.Description, c.ParentId,
                all.FirstOrDefault(x => x.Id == c.ParentId)?.Name,
                c.Products.Count,
                BuildTree(all, c.Id)))
            .ToList();
    }
}
