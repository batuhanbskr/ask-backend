using ASK.Application.Common.Interfaces;

namespace ASK.Infrastructure.Services;

/// <summary>
/// BCrypt tabanlı şifre hashleme servisi.
/// Iş faktörü 12 (2^12 iterasyon) – güvenlik/performans dengesi için optimal değer.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <summary>
    /// Zaman saldırısına karşı dayanıklı sabit zamanlı karşılaştırma.
    /// BCrypt.Verify içsel olarak sabit zamanlı karşılaştırma yapar.
    /// </summary>
    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
