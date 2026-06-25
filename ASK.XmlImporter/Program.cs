using ASK.Infrastructure.Persistence;
using ASK.XmlImporter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ─── Konfigürasyon ────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection bulunamadı.");

var sourceUrl  = config["XmlImport:SourceUrl"]
    ?? throw new InvalidOperationException("XmlImport:SourceUrl bulunamadı.");

var batchSize  = int.TryParse(config["XmlImport:BatchSize"], out var bs) ? bs : 100;

// ─── Bağımlılık enjeksiyonu ───────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

services.AddHttpClient<IXmlFetcher, HttpXmlFetcher>();
services.AddTransient<IXmlParser, XmlParser>();
services.AddTransient<IProductImporter>(sp =>
    new ProductImporter(sp.GetRequiredService<AppDbContext>(), batchSize));

var provider = services.BuildServiceProvider();

// ─── Çalıştır ─────────────────────────────────────────────────────────────────
Console.WriteLine("=== ASK XML Ürün Aktarıcı ===");
Console.WriteLine($"Kaynak : {sourceUrl}");
Console.WriteLine($"Batch  : {batchSize}");
Console.WriteLine();

try
{
    var fetcher  = provider.GetRequiredService<IXmlFetcher>();
    var parser   = provider.GetRequiredService<IXmlParser>();
    var importer = provider.GetRequiredService<IProductImporter>();

    var xml   = await fetcher.FetchAsync(sourceUrl);
    var nodes = parser.Parse(xml);

    await importer.ImportAsync(nodes);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    var current = ex;
    while (current is not null)
    {
        Console.WriteLine($"\n[HATA] {current.GetType().Name}: {current.Message}");
        current = current.InnerException;
    }
    Console.ResetColor();
    return 1;
}

Console.WriteLine("\nİşlem başarıyla tamamlandı.");
return 0;
