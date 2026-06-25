using System.Xml.Linq;
using ASK.XmlImporter.Models;

namespace ASK.XmlImporter.Services;

/// <summary>
/// XML metnini ayrıştırarak &lt;node&gt; elemanlarını XmlProductNode listesine çevirir.
/// Yeni bir alanı okumak için sadece bu sınıfı değiştirmeniz yeterlidir.
/// </summary>
public class XmlParser : IXmlParser
{
    public IReadOnlyList<XmlProductNode> Parse(string xml)
    {
        var doc = XDocument.Parse(xml);

        var nodes = doc.Descendants("node")
            .Select(n => new XmlProductNode
            {
                UrunId          = n.Element("urun_id")?.Value,
                Baslik          = n.Element("baslik")?.Value,
                Durum           = n.Element("durum")?.Value,
                Vergi           = n.Element("vergi")?.Value,
                Desi            = n.Element("desi")?.Value,
                UrunKodu        = n.Element("urun_kodu")?.Value,
                EntegrasyonKodu = n.Element("entegrasyon_kodu")?.Value,
                Barkod          = n.Element("barkod")?.Value,
                Marka           = n.Element("marka")?.Value,
                Stok            = n.Element("stok")?.Value,
                Indirim         = n.Element("indirim")?.Value,
                ParaBirimi      = n.Element("para_birimi")?.Value,
                Seo             = n.Element("seo")?.Value,
                Fiyat           = n.Element("fiyat")?.Value,
                IndirimliiFiyat = n.Element("indirimli_fiyat")?.Value,
                Kategori1       = n.Element("kategori1")?.Value,
                Kategori2       = n.Element("kategori2")?.Value,
                Link            = n.Element("link")?.Value,
                Resim1          = n.Element("resim1")?.Value,
            })
            .ToList();

        Console.WriteLine($"[Parse] {nodes.Count} ürün bulundu.");
        return nodes;
    }
}
