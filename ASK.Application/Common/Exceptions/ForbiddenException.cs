namespace ASK.Application.Common.Exceptions;

/// <summary>Yetersiz yetki durumunda fırlatılır. HTTP 403 ile eşlenir.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Bu işlem için yetkiniz bulunmamaktadır.")
        : base(message) { }
}
