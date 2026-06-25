using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Cart;
using ASK.Application.Features.Cart.Queries.GetCart;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Cart.Commands.UpdateCartItem;

public record UpdateCartItemCommand(int UserId, int CartItemId, int Quantity) : IRequest<CartDto>;

public class UpdateCartItemCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Cart), request.UserId);

        var item = cart.CartItems.FirstOrDefault(i => i.Id == request.CartItemId)
            ?? throw new NotFoundException(nameof(CartItem), request.CartItemId);

        if (request.Quantity <= 0)
        {
            cart.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Carts.Update(cart);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken)!;
        return GetCartQueryHandler.MapToDto(cart!);
    }
}
