namespace ASK.Application.DTOs.Cart;

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? ProductImageUrl,
    decimal UnitPrice,
    decimal DiscountedPrice,
    int Quantity,
    decimal LineTotal
);

public record CartDto(
    int Id,
    List<CartItemDto> Items,
    decimal SubTotal,
    int TotalItems
);

public record AddToCartDto(
    int ProductId,
    int Quantity
);

public record UpdateCartItemDto(
    int Quantity
);
