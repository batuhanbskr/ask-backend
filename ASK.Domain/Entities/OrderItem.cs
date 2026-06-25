namespace ASK.Domain.Entities;

/// <summary>
/// Sipariş kalemlerini temsil eder.
/// Ürün fiyatı sipariş anında snapshot olarak saklanır,
/// böylece sonraki fiyat değişiklikleri geçmiş siparişleri etkilemez.
/// </summary>
public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    /// <summary>Sipariş anındaki birim fiyat snapshot'ı.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Sipariş anındaki ürün adı snapshot'ı.</summary>
    public string ProductName { get; set; } = string.Empty;

    // --- Navigation Properties ---
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
