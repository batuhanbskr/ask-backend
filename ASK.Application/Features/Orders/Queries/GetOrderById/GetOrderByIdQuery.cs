using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Order;
using ASK.Application.Features.Orders.Queries.GetOrders;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(int OrderId, int UserId, bool IsAdmin) : IRequest<OrderDto>;

public class GetOrderByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // Kullanıcı yalnızca kendi siparişlerini görebilir
        if (!request.IsAdmin && order.UserId != request.UserId)
            throw new ForbiddenException();

        return GetOrdersQueryHandler.MapToDto(order);
    }
}
