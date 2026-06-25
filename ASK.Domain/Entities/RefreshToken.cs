namespace ASK.Domain.Entities;

/// <summary>
/// JWT refresh token'larını saklar.
/// Her kullanıcı birden fazla cihazda oturum açabilir.
/// Token rotation uygulanır: her yenileme eski token'ı geçersiz kılar.
/// </summary>
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }

    /// <summary>Kriptografik olarak güçlü rastgele token değeri.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Token bir kez kullanıldıktan sonra geçersiz sayılır.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Token'ın revoke edildiği zaman.</summary>
    public DateTime? RevokedAt { get; set; }

    // --- Navigation Properties ---
    public User User { get; set; } = null!;
}
