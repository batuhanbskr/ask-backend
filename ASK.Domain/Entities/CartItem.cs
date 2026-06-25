namespace ASK.Domain.Entities;

/// <summary>
/// Sepet içindeki ürün kalemlerini temsil eder.
/// </summary>
public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    // --- Navigation Properties ---
    public Cart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
