using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Order;
using ASK.Application.Features.Orders.Queries.GetOrders;
using ASK.Domain.Entities;
using ASK.Domain.Enums;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(int OrderId, OrderStatus Status) : IRequest<OrderDto>;

public class UpdateOrderStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var user = await unitOfWork.Users.GetByIdAsync(order.UserId, cancellationToken);

        var isOldCancelledOrReturned = order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Returned;
        var isNewCancelledOrReturned = request.Status == OrderStatus.Cancelled || request.Status == OrderStatus.Returned;

        // 1. Sipariş İPTAL veya İADE edildiğinde: Stoklar geri eklenir ve harcanan tutar cari bakiyeye iade edilir.
        if (isNewCancelledOrReturned && !isOldCancelledOrReturned)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                if (product is not null)
                {
                    product.Stock += item.Quantity;
                    unitOfWork.Products.Update(product);
                }
            }

            if (user is not null)
            {
                user.CurrentBalance += order.TotalAmount;
                unitOfWork.Users.Update(user);
            }
        }
        // 2. İptal/İade durumundaki sipariş tekrar AKTİF yapıldığında: Stoklar düşülür ve tutar bakiyeden tekrar düşülür.
        else if (isOldCancelledOrReturned && !isNewCancelledOrReturned)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                if (product is not null)
                {
                    product.Stock = Math.Max(0, product.Stock - item.Quantity);
                    unitOfWork.Products.Update(product);
                }
            }

            if (user is not null)
            {
                user.CurrentBalance -= order.TotalAmount;
                unitOfWork.Users.Update(user);
            }
        }

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Orders.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetOrdersQueryHandler.MapToDto(order);
    }
}
