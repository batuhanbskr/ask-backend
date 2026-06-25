using ASK.Domain.Entities;
using ASK.Infrastructure.Persistence;
using ASK.XmlImporter.Helpers;
using ASK.XmlImporter.Models;
using Microsoft.EntityFrameworkCore;

namespace ASK.XmlImporter.Services;

/// <summary>
/// XmlProductNode listesini veritabanına aktarır.
///
/// Strateji:
///   - Kategori ve Marka: isimle ara, yoksa oluştur (in-memory cache ile).
///   - Ürün: Code (urun_kodu) ile ara. Varsa güncelle, yoksa ekle.
///   - Slug: SEO alanı doluysa kullan, boşsa "{name-slug}-{code-slug}" üret.
///   - BatchSize: Her N ürün sonrası SaveChanges çağrılır, bellek baskısını azaltır.
/// </summary>
public class ProductImporter(AppDbContext db, int batchSize = 100) : IProductImporter
{
    // Ad → Id önbellekleri (her import çalışmasında DB'den bir kez yüklenir)
    private readonly Dictionary<string, int> _brandCache    = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _categoryCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task ImportAsync(IReadOnlyList<XmlProductNode> nodes, CancellationToken ct = default)
    {
        await LoadCachesAsync(ct);

        // Mevcut ürün kodlarını tek sorguda al (upsert için)
        var existingProducts = await db.Products
            .Where(p => p.Code != string.Empty)
            .ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase, ct);

        int inserted = 0, updated = 0, skipped = 0;
        int batchCount = 0;

        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.UrunKodu))
            {
                skipped++;
                continue;
            }

            var brandId    = await EnsureBrandAsync(node.Marka ?? "Diğer", ct);
            var categoryId = await EnsureCategoryAsync(node.Kategori1, node.Kategori2, ct);

            if (existingProducts.TryGetValue(node.UrunKodu, out var existing))
            {
                MapToProduct(node, existing, brandId, categoryId, keepSlug: true);
                updated++;
            }
            else
            {
                var product = new Product();
                MapToProduct(node, product, brandId, categoryId, keepSlug: false);
                db.Products.Add(product);
                inserted++;
            }

            batchCount++;
            if (batchCount >= batchSize)
            {
                await db.SaveChangesAsync(ct);
                batchCount = 0;
                Console.WriteLine($"  → Kaydedildi: {inserted} eklendi, {updated} güncellendi...");
            }
        }

        // Kalan kayıtları kaydet
        if (batchCount > 0)
            await db.SaveChangesAsync(ct);

        Console.WriteLine($"\n[Tamamlandı] Eklendi: {inserted} | Güncellendi: {updated} | Atlandı: {skipped}");
    }

    // -------------------------------------------------------------------------
    // Mapping
    // -------------------------------------------------------------------------

    private static void MapToProduct(
        XmlProductNode node,
        Product product,
        int brandId,
        int categoryId,
        bool keepSlug)
    {
        product.SupplierProductId = node.UrunId;
        product.Name              = Trim(node.Baslik) ?? "İsimsiz Ürün";
        product.Code              = Trim(node.UrunKodu)!;
        product.IntegrationCode   = Trim(node.EntegrasyonKodu);
        product.Barcode           = Trim(node.Barkod);
        product.ImageUrl          = Trim(node.Resim1);
        product.SupplierLink      = Trim(node.Link);
        product.Stock             = ParseInt(node.Stok);
        product.Price             = ParseDecimal(node.Fiyat);
        product.DiscountedPrice   = ParseDecimal(node.IndirimliiFiyat);
        product.Discount          = ParseDecimal(node.Indirim);
        product.TaxRate           = ParseDecimal(node.Vergi);
        product.Desi              = ParseDecimal(node.Desi);
        product.Currency          = Trim(node.ParaBirimi) ?? "TRY";
        product.Status            = ParseInt(node.Durum, defaultVal: 1);
        product.BrandId           = brandId;
        product.CategoryId        = categoryId;
        product.UpdatedAt         = DateTime.UtcNow;

        // Yeni eklenen ürünlerde slug + açıklama üret
        if (!keepSlug)
        {
            product.Slug             = BuildSlug(node);
            product.ShortDescription = product.Name;
            product.Description      = string.Empty;
            product.FeaturesJson     = "[]";
            product.SpecificationsJson = "{}";
            product.CreatedAt        = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Slug üretimi: SEO alanı varsa kullan, yoksa "{name-slug}-{code-slug}" üret.
    /// Kod zaten benzersiz olduğundan bu kombinasyon DB çakışması yaşatmaz.
    /// </summary>
    private static string BuildSlug(XmlProductNode node)
    {
        if (!string.IsNullOrWhiteSpace(node.Seo))
        {
            var seoSlug  = SlugHelper.Generate(node.Seo);
            var codeSlug = SlugHelper.Generate(node.UrunKodu ?? "");
            return $"{seoSlug}-{codeSlug}";
        }

        var nameSlug = SlugHelper.Generate(node.Baslik ?? "urun");
        var codePart = SlugHelper.Generate(node.UrunKodu ?? "");
        return $"{nameSlug}-{codePart}";
    }

    // -------------------------------------------------------------------------
    // Kategori ve Marka yönetimi
    // -------------------------------------------------------------------------

    private async Task<int> EnsureBrandAsync(string name, CancellationToken ct)
    {
        if (_brandCache.TryGetValue(name, out var id))
            return id;

        // DB'de aynı isimli marka var mı?
        var existing = await db.Brands
            .Where(b => b.Name == name)
            .Select(b => new { b.Id })
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            _brandCache[name] = existing.Id;
            return existing.Id;
        }

        var brand = new Brand { Name = name, IsActive = true };
        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        _brandCache[name] = brand.Id;
        return brand.Id;
    }

    private async Task<int> EnsureCategoryAsync(
        string? kategori1,
        string? kategori2,
        CancellationToken ct)
    {
        // Üst kategori
        var parentName = Trim(kategori1) ?? "Genel";
        var parentId   = await EnsureSingleCategoryAsync(parentName, null, ct);

        // Alt kategori yoksa üst kategoriyi döndür
        var subName = Trim(kategori2);
        if (string.IsNullOrEmpty(subName))
            return parentId;

        // Alt kategorinin önbellek anahtarı parent dahil benzersiz olsun
        var subKey = $"{parentName}||{subName}";
        return await EnsureSingleCategoryAsync(subKey, parentId, ct);
    }

    private async Task<int> EnsureSingleCategoryAsync(
        string cacheKey,
        int? parentId,
        CancellationToken ct)
    {
        if (_categoryCache.TryGetValue(cacheKey, out var id))
            return id;

        // Gerçek adı önbellek anahtarından ayır (parent||sub formatından)
        var displayName = cacheKey.Contains("||")
            ? cacheKey.Split("||")[1]
            : cacheKey;

        // DB'de aynı isim + parentId ile zaten var mı?
        var existing = await db.Categories
            .Where(c => c.Name == displayName && c.ParentCategoryId == parentId)
            .Select(c => new { c.Id })
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            _categoryCache[cacheKey] = existing.Id;
            return existing.Id;
        }

        // Slug benzersiz olmalı; çakışma varsa suffix ekle
        var baseSlug = SlugHelper.Generate(displayName);
        var slug     = baseSlug;
        var suffix   = 2;
        while (await db.Categories.AnyAsync(c => c.Slug == slug, ct))
            slug = $"{baseSlug}-{suffix++}";

        var category = new Category
        {
            Name             = displayName,
            Slug             = slug,
            IsActive         = true,
            ParentCategoryId = parentId,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        _categoryCache[cacheKey] = category.Id;
        return category.Id;
    }

    // -------------------------------------------------------------------------
    // Önbellek yükleme
    // -------------------------------------------------------------------------

    private async Task LoadCachesAsync(CancellationToken ct)
    {
        var brands = await db.Brands.Select(b => new { b.Id, b.Name }).ToListAsync(ct);
        foreach (var b in brands)
            _brandCache[b.Name] = b.Id;

        var categories = await db.Categories
            .Select(c => new { c.Id, c.Name, c.ParentCategoryId })
            .ToListAsync(ct);

        // Önbellek anahtarı: kök kategoriler için sadece ad, alt kategoriler için "parent||alt"
        foreach (var c in categories)
        {
            if (c.ParentCategoryId is null)
            {
                _categoryCache[c.Name] = c.Id;
            }
            else
            {
                var parent = categories.FirstOrDefault(p => p.Id == c.ParentCategoryId);
                if (parent is not null)
                    _categoryCache[$"{parent.Name}||{c.Name}"] = c.Id;
            }
        }

        Console.WriteLine($"[Cache] {brands.Count} marka, {categories.Count} kategori yüklendi.");
    }

    // -------------------------------------------------------------------------
    // Dönüşüm yardımcıları
    // -------------------------------------------------------------------------

    private static string? Trim(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal ParseDecimal(string? value)
        => decimal.TryParse(value, System.Globalization.NumberStyles.Any,
               System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result : 0m;

    private static int ParseInt(string? value, int defaultVal = 0)
        => int.TryParse(value, out var result) ? result : defaultVal;
}
