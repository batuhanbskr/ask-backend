namespace ASK.Domain.Enums;

public enum PaymentStatus
{
    Pending   = 0, // Beklemede
    Completed = 1, // Tamamlandı
    Failed    = 2, // Başarısız
    Refunded  = 3  // İade edildi
}
