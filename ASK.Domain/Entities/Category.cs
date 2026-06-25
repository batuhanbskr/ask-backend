namespace ASK.Domain.Entities;

/// <summary>
/// Ürün kategorilerini temsil eder.
/// Üst-alt kategori hiyerarşisini destekler (örn: Havalı Aletler > Tornavidalar).
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>URL dostu benzersiz tanımlayıcı (örn: havali-aletler).</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Kategorinin öne çıkan görseli veya ikon sınıfı.</summary>
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Üst kategori ID'si. Null ise kök kategoridir.</summary>
    public int? ParentCategoryId { get; set; }

    // --- Navigation Properties ---
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
