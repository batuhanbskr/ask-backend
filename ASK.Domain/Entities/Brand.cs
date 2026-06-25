namespace ASK.Domain.Entities;

/// <summary>
/// Ürün markalarını temsil eder (örn: DeWalt, Maier, NT Tools).
/// </summary>
public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;

    // --- Navigation Properties ---
    public ICollection<Product> Products { get; set; } = [];
}
