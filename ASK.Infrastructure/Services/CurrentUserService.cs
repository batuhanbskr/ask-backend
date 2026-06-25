using System.Security.Claims;
using ASK.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ASK.Infrastructure.Services;

/// <summary>
/// HTTP context'ten JWT claim'lerini okuyarak mevcut kullanıcı bilgisini sağlar.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var sub = _user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _user?.FindFirstValue("sub");
            return int.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => _user?.FindFirstValue(ClaimTypes.Email)
        ?? _user?.FindFirstValue("email");

    public string? Role => _user?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;
}
