namespace ASK.Domain.Entities;

/// <summary>
/// Ürün entity'si.
/// Tedarikçi XML yapısına (urun_id, entegrasyon_kodu, barkod vb.) tam uyumludur.
/// </summary>
public class Product : BaseEntity
{
    /// <summary>Tedarikçi tarafındaki ürün numarası (urun_id: 170604).</summary>
    public string? SupplierProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>URL dostu benzersiz tanımlayıcı (SEO amaçlı).</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Kısa ürün açıklaması, listing sayfalarında gösterilir.</summary>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>Uzun HTML/Markdown ürün açıklaması, detay sayfasında gösterilir.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ürün kodu (örn: DW7GK01).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Entegrasyon kodu (örn: 00000-DW7GK01).</summary>
    public string? IntegrationCode { get; set; }

    /// <summary>Barkod numarası.</summary>
    public string? Barcode { get; set; }

    /// <summary>Ana ürün görseli URL'i.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Ürün özellikleri JSON olarak saklanır.
    /// Örn: ["3 kademeli hız ayarı", "Hafif kompakt gövde"]
    /// </summary>
    public string FeaturesJson { get; set; } = "[]";

    /// <summary>
    /// Teknik özellikler JSON olarak saklanır.
    /// Örn: {"Çap":"6mm","Ağırlık":"0.4kg"}
    /// </summary>
    public string SpecificationsJson { get; set; } = "{}";

    /// <summary>Stok adedi.</summary>
    public int Stock { get; set; }

    /// <summary>Normal satış fiyatı.</summary>
    public decimal Price { get; set; }

    /// <summary>İndirimli fiyat. İndirim yoksa Price ile aynı değer taşır.</summary>
    public decimal DiscountedPrice { get; set; }

    /// <summary>İndirim yüzdesi (0-100).</summary>
    public decimal Discount { get; set; }

    /// <summary>KDV oranı (örn: 20.00).</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Desi (kargo hacim ağırlığı).</summary>
    public decimal Desi { get; set; }

    /// <summary>Para birimi (USD, TRY, EUR).</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>Aktif/Pasif durumu. 1=Aktif, 0=Pasif.</summary>
    public int Status { get; set; } = 1;

    /// <summary>Yeni ürün etiketi için işaret bayrağı.</summary>
    public bool IsNew { get; set; }

    /// <summary>Öne çıkan ürün bayrağı (anasayfada gösterilir).</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Tedarikçi ürün linki.</summary>
    public string? SupplierLink { get; set; }

    // --- Foreign Keys ---
    public int CategoryId { get; set; }
    public int BrandId { get; set; }

    // --- Navigation Properties ---
    public Category Category { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];
}
