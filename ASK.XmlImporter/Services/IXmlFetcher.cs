namespace ASK.XmlImporter.Services;

/// <summary>
/// Verilen URL'den ham XML içeriğini getirir.
/// </summary>
public interface IXmlFetcher
{
    Task<string> FetchAsync(string url, CancellationToken ct = default);
}
