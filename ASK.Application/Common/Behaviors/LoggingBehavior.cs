using MediatR;
using Microsoft.Extensions.Logging;

namespace ASK.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline davranışı: her request/response çifti için performans ve hata loglaması yapar.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("İşlem başladı: {RequestName}", requestName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();
            logger.LogInformation("İşlem tamamlandı: {RequestName} ({ElapsedMs}ms)",
                requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "İşlem başarısız: {RequestName} ({ElapsedMs}ms)",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
