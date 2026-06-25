using ASK.Application.Common.Exceptions;
using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Product;
using ASK.Application.Features.Products.Queries.GetProducts;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Products.Queries.GetProductBySlug;

public record GetProductBySlugQuery(string Slug) : IRequest<ProductDto>;

public class GetProductBySlugQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetProductBySlugQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Slug);

        decimal discountRate = 0;
        if (currentUser.IsAuthenticated && currentUser.UserId.HasValue)
        {
            var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId.Value, cancellationToken);
            if (user != null)
            {
                discountRate = user.GlobalDiscountRate;
            }
        }

        return GetProductsQueryHandler.MapToDto(product, discountRate);
    }
}
