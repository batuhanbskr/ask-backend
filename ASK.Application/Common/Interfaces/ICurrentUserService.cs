namespace ASK.Application.Common.Interfaces;

/// <summary>
/// HTTP context'ten mevcut oturum açmış kullanıcı bilgisini soyutlar.
/// Infrastructure katmanında HttpContextAccessor ile implemente edilir.
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
