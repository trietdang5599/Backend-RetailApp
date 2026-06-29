namespace ProductManagement.Application.Interfaces;

public interface IProductAttributeService
{
    Task<string?> GetAttributesAsync(string documentId, CancellationToken ct = default);
    Task<string> SaveAttributesAsync(Guid productId, Dictionary<string, object> attributes, CancellationToken ct = default);
    Task UpdateAttributesAsync(string documentId, Dictionary<string, object> attributes, CancellationToken ct = default);
    Task DeleteAttributesAsync(string documentId, CancellationToken ct = default);
}
