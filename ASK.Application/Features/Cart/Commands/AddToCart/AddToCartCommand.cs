using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Cart;
using ASK.Application.Features.Cart.Queries.GetCart;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using FluentValidation;
using MediatR;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;
using DomainCart = ASK.Domain.Entities.Cart;

namespace ASK.Application.Features.Cart.Commands.AddToCart;

public record AddToCartCommand(int UserId, int ProductId, int Quantity) : IRequest<CartDto>;

public class AddToCartCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<AddToCartCommand, CartDto>
{
    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (product.Status != 1)
            throw new AppValidationException($"'{product.Name}' ürünü aktif değil.");

        if (product.Stock < request.Quantity)
            throw new AppValidationException($"Yeterli stok yok. Mevcut: {product.Stock}");

        var cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken);

        if (cart is null)
        {
            cart = new DomainCart { UserId = request.UserId };
            await unitOfWork.Carts.AddAsync(cart, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken)!;
        }

        var existingItem = cart!.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);

        if (existingItem is not null)
        {
            // Var olan kalemi güncelle
            existingItem.Quantity += request.Quantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Carts.Update(cart);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken)!;
        return GetCartQueryHandler.MapToDto(cart!);
    }
}

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(999);
    }
}
