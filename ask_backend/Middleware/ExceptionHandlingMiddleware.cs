using System.Net;
using System.Text.Json;
using ASK.Application.Common.Exceptions;
using AppValidationException = ASK.Application.Common.Exceptions.ValidationException;

namespace ask_backend.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Domain exception'larını uygun HTTP status kodlarına dönüştürür.
/// Ham exception detaylarını istemciye sızdırmaz (OWASP A05).
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "İşlenmeyen exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException nfe =>
                (HttpStatusCode.NotFound, nfe.Message, (object?)null),

            AppValidationException ve =>
                (HttpStatusCode.BadRequest, ve.Message, (object?)ve.Errors),

            UnauthorizedException ue =>
                (HttpStatusCode.Unauthorized, ue.Message, (object?)null),

            ForbiddenException fe =>
                (HttpStatusCode.Forbidden, fe.Message, (object?)null),

            // Bilinmeyen exception'larda iç detayları gizle
            _ => (HttpStatusCode.InternalServerError,
                  "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.",
                  (object?)null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success = false,
            message,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
