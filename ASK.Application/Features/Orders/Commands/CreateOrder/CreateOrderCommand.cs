using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Order;
using ASK.Application.Features.Orders.Queries.GetOrders;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using FluentValidation;
using MediatR;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;
using DomainCart = ASK.Domain.Entities.Cart;

namespace ASK.Application.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(int UserId, string ShippingAddress, string? Notes) : IRequest<OrderDto>;

public class CreateOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Kullanıcının sepetini al
        var cart = await unitOfWork.Carts.GetByUserIdWithItemsAsync(request.UserId, cancellationToken)
            ?? throw new AppValidationException("Sepet bulunamadı.");

        if (!cart.CartItems.Any())
            throw new AppValidationException("Sepet boş. Sipariş oluşturulamaz.");

        // Stok ve ürün geçerliliği kontrolü
        foreach (var cartItem in cart.CartItems)
        {
            var product = await unitOfWork.Products.GetByIdAsync(cartItem.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), cartItem.ProductId);

            if (product.Status != 1)
                throw new AppValidationException($"'{product.Name}' ürünü aktif değil.");

            if (product.Stock < cartItem.Quantity)
                throw new AppValidationException($"'{product.Name}' için yeterli stok yok. Mevcut: {product.Stock}");
        }

        var orderNumber = await unitOfWork.Orders.GenerateOrderNumberAsync(cancellationToken);

        var order = new Order
        {
            UserId = request.UserId,
            OrderNumber = orderNumber,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes
        };

        decimal total = 0;

        foreach (var cartItem in cart.CartItems)
        {
            var product = await unitOfWork.Products.GetByIdAsync(cartItem.ProductId, cancellationToken)!;
            var unitPrice = product!.DiscountedPrice;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = product.Name,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice
            });

            // Stok düş
            product.Stock -= cartItem.Quantity;
            unitOfWork.Products.Update(product);

            total += unitPrice * cartItem.Quantity;
        }

        order.TotalAmount = total;

        await unitOfWork.Orders.AddAsync(order, cancellationToken);

        // Sepeti temizle
        unitOfWork.Carts.Remove(cart);
        var newCart = new DomainCart { UserId = request.UserId };
        await unitOfWork.Carts.AddAsync(newCart, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetOrdersQueryHandler.MapToDto(order);
    }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Teslimat adresi boş olamaz.")
            .MaximumLength(500);
    }
}
