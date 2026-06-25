using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Queries.GetLowStockProducts;

public record GetLowStockProductsQuery(int MaxStock = 10) : IRequest<List<ProductDto>>;

public class GetLowStockProductsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetLowStockProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await unitOfWork.Products.GetLowStockAsync(request.MaxStock, cancellationToken);

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
