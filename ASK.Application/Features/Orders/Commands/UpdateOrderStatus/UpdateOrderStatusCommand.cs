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

        // İptal edilen siparişlerde stok geri iade edilir
        if (request.Status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
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
        }

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Orders.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetOrdersQueryHandler.MapToDto(order);
    }
}
