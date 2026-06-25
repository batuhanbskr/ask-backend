using ASK.XmlImporter.Models;

namespace ASK.XmlImporter.Services;

/// <summary>
/// Ham XML metnini ayrıştırarak XmlProductNode listesine dönüştürür.
/// </summary>
public interface IXmlParser
{
    IReadOnlyList<XmlProductNode> Parse(string xml);
}
