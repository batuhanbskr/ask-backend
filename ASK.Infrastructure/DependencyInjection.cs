using ASK.Application.Common.Interfaces;
using ASK.Application.Common.Models;
using ASK.Domain.Interfaces;
using ASK.Infrastructure.Persistence;
using ASK.Infrastructure.Persistence.Repositories;
using ASK.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ASK.Infrastructure;

/// <summary>
/// Infrastructure katmanı servis kayıtları.
/// API katmanından tek satırla çağrılır: services.AddInfrastructure(configuration)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MySQL bağlantısı (Pomelo EF Core provider)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mysqlOptions => mysqlOptions.EnableRetryOnFailure(3)));

        // Unit of Work ve Repository'ler
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Uygulama servisleri
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();

        // JWT ayarları
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        return services;
    }
}
