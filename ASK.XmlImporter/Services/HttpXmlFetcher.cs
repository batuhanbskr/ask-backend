using System.IO;

namespace ASK.XmlImporter.Services;

/// <summary>
/// HTTP veya yerel dosya sistemi üzerinden XML belgesi getirir.
/// </summary>
public class HttpXmlFetcher(HttpClient httpClient) : IXmlFetcher
{
    public async Task<string> FetchAsync(string url, CancellationToken ct = default)
    {
        Console.WriteLine($"[Fetch] {url}");
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
        else
        {
            return await File.ReadAllTextAsync(url, ct);
        }
    }
}
