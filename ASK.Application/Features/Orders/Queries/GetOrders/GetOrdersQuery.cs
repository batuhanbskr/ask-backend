using ASK.Application.Common.Exceptions;
using ASK.Application.DTOs.Order;
using ASK.Domain.Interfaces;
using MediatR;

namespace ASK.Application.Features.Orders.Queries.GetOrders;

public record GetOrdersQuery(int UserId, bool IsAdmin) : IRequest<List<OrderDto>>;

public class GetOrdersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = request.IsAdmin
            ? await unitOfWork.Orders.GetAllAsync(cancellationToken)
            : await unitOfWork.Orders.GetByUserIdAsync(request.UserId, cancellationToken);

        return orders.Select(MapToDto).ToList();
    }

    internal static OrderDto MapToDto(Domain.Entities.Order o) => new(
        o.Id, o.OrderNumber, o.Status,
        GetStatusLabel(o.Status),
        o.TotalAmount, o.ShippingAddress, o.Notes, o.CreatedAt,
        o.OrderItems.Select(i => new OrderItemDto(
            i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice
        )).ToList()
    );

    private static string GetStatusLabel(Domain.Enums.OrderStatus status) => status switch
    {
        Domain.Enums.OrderStatus.Pending => "Beklemede",
        Domain.Enums.OrderStatus.Confirmed => "Onaylandı",
        Domain.Enums.OrderStatus.Shipped => "Kargoda",
        Domain.Enums.OrderStatus.Delivered => "Teslim Edildi",
        Domain.Enums.OrderStatus.Cancelled => "İptal Edildi",
        _ => "Bilinmiyor"
    };
}
