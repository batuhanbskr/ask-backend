using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Cart;
using ASK.Application.Features.Cart.Queries.GetCart;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;
using DomainCart = ASK.Domain.Entities.Cart;

namespace ASK.Application.Features.Cart.Commands.ClearCart;

public record ClearCartCommand(int UserId) : IRequest<CartDto>;

public class ClearCartCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ClearCartCommand, CartDto>
{
    public async Task<CartDto> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken);

        if (cart is null)
        {
            var newCart = new DomainCart { UserId = request.UserId };
            await unitOfWork.Carts.AddAsync(newCart, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return GetCartQueryHandler.MapToDto(newCart);
        }

        cart.CartItems.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Carts.Update(cart);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetCartQueryHandler.MapToDto(cart);
    }
}
