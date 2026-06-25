using ASK.Domain.Enums;

namespace ASK.Application.DTOs.Order;

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record OrderDto(
    int Id,
    string OrderNumber,
    OrderStatus Status,
    string StatusLabel,
    decimal TotalAmount,
    string ShippingAddress,
    string? Notes,
    DateTime CreatedAt,
    List<OrderItemDto> Items
);

public record CreateOrderDto(
    string ShippingAddress,
    string? Notes
);

public record UpdateOrderStatusDto(
    OrderStatus Status
);
