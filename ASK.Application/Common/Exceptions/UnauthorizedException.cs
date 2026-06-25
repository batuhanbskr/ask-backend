namespace ASK.Application.Common.Exceptions;

/// <summary>Kimlik doğrulama başarısız olduğunda fırlatılır. HTTP 401 ile eşlenir.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Bu işlem için kimlik doğrulaması gereklidir.")
        : base(message) { }
}
