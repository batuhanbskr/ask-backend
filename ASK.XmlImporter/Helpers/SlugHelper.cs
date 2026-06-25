using System.Text;
using System.Text.RegularExpressions;

namespace ASK.XmlImporter.Helpers;

/// <summary>
/// Metin tabanlı bir slug üretir. Türkçe karakterleri Latin karşılıklarına çevirir.
/// Örn: "Çok Amaçlı Kaynakçı Kumpası" → "cok-amacli-kaynakci-kumpasi"
/// </summary>
public static partial class SlugHelper
{
    private static readonly Dictionary<char, char> TurkishMap = new()
    {
        ['ç'] = 'c', ['Ç'] = 'c',
        ['ğ'] = 'g', ['Ğ'] = 'g',
        ['ı'] = 'i', ['İ'] = 'i',
        ['ö'] = 'o', ['Ö'] = 'o',
        ['ş'] = 's', ['Ş'] = 's',
        ['ü'] = 'u', ['Ü'] = 'u',
    };

    /// <summary>
    /// Verilen metni URL uyumlu slug'a çevirir.
    /// Benzersizlik garantisi yoktur; ProductImporter gerektiğinde suffix ekler.
    /// </summary>
    public static string Generate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "urun";

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
            sb.Append(TurkishMap.TryGetValue(c, out var mapped) ? mapped : c);

        var slug = sb.ToString()
                     .ToLowerInvariant()
                     .Replace(' ', '-');

        // Harf, rakam ve tire dışındaki karakterleri sil
        slug = NonAlphanumericHyphen().Replace(slug, string.Empty);

        // Art arda gelen tireleri teke düşür
        slug = MultipleHyphens().Replace(slug, "-");

        return slug.Trim('-');
    }

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex NonAlphanumericHyphen();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphens();
}
