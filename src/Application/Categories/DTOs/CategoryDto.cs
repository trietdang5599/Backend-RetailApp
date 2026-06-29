namespace ProductManagement.Application.Categories.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentId,
    string? ParentName,
    int ProductCount,
    List<CategoryDto> Children
);
