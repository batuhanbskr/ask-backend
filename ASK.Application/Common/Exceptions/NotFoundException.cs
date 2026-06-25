namespace ASK.Application.Common.Exceptions;

/// <summary>Entity bulunamadığında fırlatılır. HTTP 404 ile eşlenir.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"'{entityName}' ({key}) bulunamadı.") { }
}
