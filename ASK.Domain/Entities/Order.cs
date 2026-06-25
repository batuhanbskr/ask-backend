using ASK.Domain.Enums;

namespace ASK.Domain.Entities;

/// <summary>
/// Müşteri siparişlerini temsil eder.
/// Sipariş bir kez oluşturulduktan sonra fiyatlar değişmemeli,
/// bu yüzden OrderItem üzerinde birim fiyat tutulur.
/// </summary>
public class Order : BaseEntity
{
    /// <summary>Okunabilir sipariş numarası (örn: ASK-20240511-00042).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Sipariş toplam tutarı (vergi dahil).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Teslimat adresi (sipariş anındaki snapshot).</summary>
    public string ShippingAddress { get; set; } = string.Empty;

    public string? Notes { get; set; }

    // --- Foreign Keys ---
    public int UserId { get; set; }

    // --- Navigation Properties ---
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
