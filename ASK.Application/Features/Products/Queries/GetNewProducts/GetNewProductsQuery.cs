using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Queries.GetNewProducts;

public record GetNewProductsQuery : IRequest<List<ProductDto>>;

public class GetNewProductsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetNewProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetNewProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await unitOfWork.Products.GetNewAsync(cancellationToken);

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
