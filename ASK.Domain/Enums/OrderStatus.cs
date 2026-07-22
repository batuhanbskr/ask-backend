namespace ASK.Domain.Enums;

/// <summary>
/// Sipariş durum akışı.
/// Pending → Confirmed → Shipped → Delivered
/// Herhangi bir aşamada Cancelled veya ReturnRequested olabilir.
/// </summary>
public enum OrderStatus
{
    Pending = 0,            // Sipariş oluşturuldu, onay bekleniyor
    Confirmed = 1,          // Sipariş onaylandı, hazırlanıyor
    Shipped = 2,            // Kargoya verildi
    Delivered = 3,          // Teslim edildi
    Cancelled = 4,          // İptal edildi
    ReturnRequested = 5,    // İade Talebi Oluşturuldu (Danışmana Mail Gider)
    Returned = 6            // İade Edildi (Bakiyeye İade Yansıtıldı)
}
