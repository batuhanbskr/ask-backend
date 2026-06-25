using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Queries.GetFeaturedProducts;

public record GetFeaturedProductsQuery : IRequest<List<ProductDto>>;

public class GetFeaturedProductsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetFeaturedProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await unitOfWork.Products.GetFeaturedAsync(cancellationToken);
        return products.Select(GetProductsQueryHandler.MapToDto).ToList();
    }
}
