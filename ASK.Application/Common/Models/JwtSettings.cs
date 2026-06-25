namespace ASK.Application.Common.Models;

/// <summary>JWT ayarları. appsettings.json'dan bind edilir.</summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Access token geçerlilik süresi (dakika).</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>Refresh token geçerlilik süresi (gün).</summary>
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
