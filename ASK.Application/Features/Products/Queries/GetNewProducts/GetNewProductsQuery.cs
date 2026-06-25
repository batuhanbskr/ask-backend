using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Queries.GetNewProducts;

public record GetNewProductsQuery : IRequest<List<ProductDto>>;

public class GetNewProductsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetNewProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetNewProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await unitOfWork.Products.GetNewAsync(cancellationToken);
        return products.Select(GetProductsQueryHandler.MapToDto).ToList();
    }
}
