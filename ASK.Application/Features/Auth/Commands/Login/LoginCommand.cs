using ASK.Application.Common.Exceptions;
using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Auth;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;
using DomainRefreshToken = ASK.Domain.Entities.RefreshToken;

namespace ASK.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;

public class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Güvenlik: Hatalı email/şifrede aynı hata mesajı verilir (enumeration saldırısını önler)
        var user = await unitOfWork.Users.GetByEmailAsync(
            request.Email.ToLowerInvariant().Trim(), cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email veya şifre hatalı.");

        // Pasif kullanıcılar da sisteme giriş yapabilir, ürünleri inceleyip sepete ekleyebilir;
        // ancak sipariş ve ödeme yapmaları engellenecektir.

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();

        // Eski refresh token'ları iptal et (rotation)
        await unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(user.Id, cancellationToken);

        var refreshToken = new DomainRefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        await unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.Role.ToString(), accessToken, refreshTokenValue,
            DateTime.UtcNow.AddHours(1),
            user.CurrentBalance,
            user.SalesRepresentative != null ? new SalesRepresentativeDto(
                user.SalesRepresentative.FirstName,
                user.SalesRepresentative.LastName,
                user.SalesRepresentative.Email,
                user.SalesRepresentative.Phone
            ) : null,
            user.IsActive);
    }
}
