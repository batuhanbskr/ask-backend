namespace ASK.Application.Common.Interfaces;

/// <summary>
/// Şifre hashleme ve doğrulama işlemlerini soyutlar.
/// Infrastructure katmanında BCrypt ile implemente edilir.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
