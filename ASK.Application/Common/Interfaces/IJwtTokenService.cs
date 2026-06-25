using ASK.Domain.Entities;

namespace ASK.Application.Common.Interfaces;

/// <summary>
/// JWT access ve refresh token üretimini soyutlar.
/// Infrastructure katmanında System.IdentityModel.Tokens.Jwt ile implemente edilir.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int? GetUserIdFromToken(string token);
}
