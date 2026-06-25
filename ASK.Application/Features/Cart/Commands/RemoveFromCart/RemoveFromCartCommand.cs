using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Cart;
using ASK.Application.Features.Cart.Queries.GetCart;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Cart.Commands.RemoveFromCart;

public record RemoveFromCartCommand(int UserId, int CartItemId) : IRequest<CartDto>;

public class RemoveFromCartCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveFromCartCommand, CartDto>
{
    public async Task<CartDto> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Cart), request.UserId);

        var item = cart.CartItems.FirstOrDefault(i => i.Id == request.CartItemId)
            ?? throw new NotFoundException(nameof(CartItem), request.CartItemId);

        cart.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Carts.Update(cart);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken)!;
        return GetCartQueryHandler.MapToDto(cart!);
    }
}
