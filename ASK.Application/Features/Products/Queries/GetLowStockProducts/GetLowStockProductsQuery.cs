using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Queries.GetLowStockProducts;

public record GetLowStockProductsQuery(int MaxStock = 10) : IRequest<List<ProductDto>>;

public class GetLowStockProductsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLowStockProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await unitOfWork.Products.GetLowStockAsync(request.MaxStock, cancellationToken);
        return products.Select(GetProductsQueryHandler.MapToDto).ToList();
    }
}
