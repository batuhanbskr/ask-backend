using ASK.Application.Common.Exceptions;
using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Auth;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;
using DomainRefreshToken = ASK.Domain.Entities.RefreshToken;

namespace ASK.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;

public class RefreshTokenCommandHandler(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await unitOfWork.RefreshTokens.GetActiveTokenAsync(
            request.RefreshToken, cancellationToken);

        if (storedToken is null)
            throw new UnauthorizedException("Geçersiz veya süresi dolmuş refresh token.");

        var user = await unitOfWork.Users.GetByIdAsync(storedToken.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Kullanıcı bulunamadı.");

        if (!user.IsActive)
            throw new UnauthorizedException("Hesabınız devre dışı bırakılmıştır.");

        // Token rotation: eski token'ı iptal et
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        unitOfWork.RefreshTokens.Update(storedToken);

        var newAccessToken = jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = jwtTokenService.GenerateRefreshToken();

        var newRefreshToken = new DomainRefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        await unitOfWork.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.Role.ToString(), newAccessToken, newRefreshTokenValue,
            DateTime.UtcNow.AddHours(1));
    }
}
