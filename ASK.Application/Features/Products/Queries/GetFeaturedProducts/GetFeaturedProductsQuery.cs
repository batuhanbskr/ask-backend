using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Queries.GetFeaturedProducts;

public record GetFeaturedProductsQuery : IRequest<List<ProductDto>>;

public class GetFeaturedProductsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetFeaturedProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await unitOfWork.Products.GetFeaturedAsync(cancellationToken);

        decimal discountRate = 0;
        if (currentUser.IsAuthenticated && currentUser.UserId.HasValue)
        {
            var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId.Value, cancellationToken);
            if (user != null)
            {
                discountRate = user.GlobalDiscountRate;
            }
        }

        return products.Select(p => GetProductsQueryHandler.MapToDto(p, discountRate)).ToList();
    }
}
