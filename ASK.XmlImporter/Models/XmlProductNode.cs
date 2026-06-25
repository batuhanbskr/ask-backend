namespace ASK.XmlImporter.Models;

/// <summary>
/// XML'deki her &lt;node&gt; elemanını birebir temsil eder.
/// Alanlar XML'den okunduğu gibi string/nullable tutulur; dönüşümler mapper katmanında yapılır.
/// </summary>
public class XmlProductNode
{
    public string? UrunId          { get; set; }   // <urun_id>
    public string? Baslik          { get; set; }   // <baslik>
    public string? Durum           { get; set; }   // <durum>  1=aktif 0=pasif
    public string? Vergi           { get; set; }   // <vergi>  KDV oranı
    public string? Desi            { get; set; }   // <desi>
    public string? UrunKodu        { get; set; }   // <urun_kodu>
    public string? EntegrasyonKodu { get; set; }   // <entegrasyon_kodu>
    public string? Barkod          { get; set; }   // <barkod>
    public string? Marka           { get; set; }   // <marka>
    public string? Stok            { get; set; }   // <stok>
    public string? Indirim         { get; set; }   // <indirim>
    public string? ParaBirimi      { get; set; }   // <para_birimi>
    public string? Seo             { get; set; }   // <seo>
    public string? Fiyat           { get; set; }   // <fiyat>
    public string? IndirimliiFiyat { get; set; }   // <indirimli_fiyat>
    public string? Kategori1       { get; set; }   // <kategori1>  üst kategori
    public string? Kategori2       { get; set; }   // <kategori2>  alt kategori
    public string? Link            { get; set; }   // <link>  tedarikçi ürün linki
    public string? Resim1          { get; set; }   // <resim1>  görsel URL
}
