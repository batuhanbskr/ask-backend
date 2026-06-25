using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ASK.Infrastructure.Persistence;

/// <summary>
/// EF Core migration araçları için tasarım zamanı fabrikası.
/// Gerçek bağlantıya ihtiyaç duymadan migration oluşturmayı sağlar.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Migration oluşturma sırasında geçici bağlantı dizesi kullanılır.
        // Gerçek bağlantı appsettings.json'dan okunur.
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3306;Database=ask_ecommerce;User=root;Password=admin;CharSet=utf8mb4;",
            ServerVersion.AutoDetect("Server=localhost;Port=3306;Database=ask_ecommerce;User=root;Password=admin;CharSet=utf8mb4;"),
            x => x.MigrationsAssembly("ASK.Infrastructure"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
