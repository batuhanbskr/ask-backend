using ASK.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ASK.Application;

/// <summary>
/// Application katmanı servis kayıtları.
/// API katmanından tek satırla çağrılır: services.AddApplication()
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR – tüm handler'ları otomatik tarar
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // FluentValidation – tüm validator'ları otomatik tarar
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Pipeline davranışları (sıra önemli: önce loglama, sonra validation)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
