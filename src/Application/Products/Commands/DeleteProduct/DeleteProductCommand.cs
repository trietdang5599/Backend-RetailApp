using MediatR;
using ProductManagement.Application.Common;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;

    public DeleteProductHandler(IUnitOfWork uow, ICacheService cache)
    {
        _uow = uow;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        if (!await _uow.Products.ExistsAsync(request.Id, ct))
            return Result<bool>.Failure("Product not found");

        await _uow.Products.DeleteAsync(request.Id, ct);
        await _uow.SaveChangesAsync(ct);

        await _cache.RemoveByPatternAsync("products:*", ct);
        await _cache.RemoveAsync($"product:{request.Id}", ct);

        return Result<bool>.Success(true);
    }
}
