namespace ASK.Domain.Enums;

/// <summary>
/// Sipariş durum akışı.
/// Pending → Confirmed → Shipped → Delivered
/// Herhangi bir aşamada Cancelled olabilir.
/// </summary>
public enum OrderStatus
{
    Pending = 0,       // Sipariş oluşturuldu, ödeme bekleniyor
    Confirmed = 1,     // Sipariş onaylandı, hazırlanıyor
    Shipped = 2,       // Kargoya verildi
    Delivered = 3,     // Teslim edildi
    Cancelled = 4      // İptal edildi
}
