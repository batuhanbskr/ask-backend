using FluentValidation;
using MediatR;

namespace ASK.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline davranışı: her Command/Query önce FluentValidation ile doğrulanır.
/// Doğrulama hatası varsa handler çağrılmaz, ValidationException fırlatılır.
/// Bu, cross-cutting validation concern'ünü handler'lardan ayırır (SOLID - SRP).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.ToArray());

            throw new Exceptions.ValidationException(errors);
        }

        return await next();
    }
}
