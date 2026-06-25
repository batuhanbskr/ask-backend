using ASK.XmlImporter.Models;

namespace ASK.XmlImporter.Services;

/// <summary>
/// XmlProductNode listesini veritabanına aktarır.
/// Var olan kayıtları günceller (urun_kodu ile eşleşme), olmayanları ekler.
/// </summary>
public interface IProductImporter
{
    Task ImportAsync(IReadOnlyList<XmlProductNode> nodes, CancellationToken ct = default);
}
