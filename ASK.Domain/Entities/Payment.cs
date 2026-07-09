using ASK.Domain.Enums;

namespace ASK.Domain.Entities;

/// <summary>
/// Dükkan içi ödeme kayıtları.
/// Nakit, kredi kartı, sanal POS, havale/EFT ve çek ödemelerini takip eder.
/// Bir ödeme isteğe bağlı olarak bir siparişe ve/veya kullanıcıya bağlanabilir.
/// </summary>
public class Payment : BaseEntity
{
    /// <summary>Okunabilir ödeme numarası (örn: PAY-20240511-00001).</summary>
    public string PaymentNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Açıklama / Not.</summary>
    public string? Description { get; set; }

    /// <summary>POS referans no, banka işlem no, çek no vb.</summary>
    public string? Reference { get; set; }

    /// <summary>Ödemenin yapıldığı tarih (işlem zamanı).</summary>
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    // --- Foreign Keys (opsiyonel) ---
    public int? UserId { get; set; }

    // --- Navigation Properties ---
    public User? User { get; set; }
}
