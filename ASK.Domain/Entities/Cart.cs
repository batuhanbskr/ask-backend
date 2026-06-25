namespace ASK.Domain.Entities;

/// <summary>
/// Kullanıcı alışveriş sepetini temsil eder.
/// Her kullanıcının en fazla bir aktif sepeti olabilir.
/// </summary>
public class Cart : BaseEntity
{
    public int UserId { get; set; }

    // --- Navigation Properties ---
    public User User { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = [];
}
