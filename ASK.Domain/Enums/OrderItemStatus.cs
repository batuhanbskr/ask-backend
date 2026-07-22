namespace ASK.Domain.Enums;

public enum OrderItemStatus
{
    Pending = 0,    // Beklemede
    Approved = 1,   // Onaylandı (Sipariş Tutarına Yansır)
    Cancelled = 2   // İptal (Sipariş Tutarına Yansımaz)
}
