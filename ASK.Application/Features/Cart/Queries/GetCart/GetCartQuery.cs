using ASK.Application.DTOs.Cart;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Cart.Queries.GetCart;

public record GetCartQuery(int UserId) : IRequest<CartDto>;

public class GetCartQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken);

        if (cart is null)
        {
            // Sepet yoksa oluştur
            cart = new Domain.Entities.Cart { UserId = request.UserId };
            await unitOfWork.Carts.AddAsync(cart, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(cart);
    }

    internal static CartDto MapToDto(Domain.Entities.Cart cart)
    {
        var globalDiscountRate = cart.User?.GlobalDiscountRate ?? 0;

        var items = cart.CartItems.Select(ci => {
            var price = ci.Product?.Price ?? 0;
            var discountedPrice = ci.Product?.DiscountedPrice ?? 0;

            if (discountedPrice <= 0 || discountedPrice > price)
            {
                discountedPrice = price;
            }

            if (globalDiscountRate > 0)
            {
                discountedPrice = discountedPrice * (1 - globalDiscountRate / 100);
            }

            return new CartItemDto(
                ci.Id,
                ci.ProductId,
                ci.Product?.Name ?? string.Empty,
                ci.Product?.ImageUrl,
                price,
                discountedPrice,
                ci.Quantity,
                discountedPrice * ci.Quantity
            );
        }).ToList();

        return new CartDto(
            cart.Id,
            items,
            items.Sum(i => i.LineTotal),
            items.Sum(i => i.Quantity)
        );
    }
}
