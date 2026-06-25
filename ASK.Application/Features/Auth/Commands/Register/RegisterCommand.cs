using ASK.Application.Common.Exceptions;
using ASK.Application.Common.Interfaces;
using ASK.Application.DTOs.Auth;
using ASK.Domain.Entities;
using ASK.Domain.Interfaces;
using MediatR;
using DomainCart = ASK.Domain.Entities.Cart;
using DomainRefreshToken = ASK.Domain.Entities.RefreshToken;

namespace ASK.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    string? Company
) : IRequest<AuthResponseDto>;

public class RegisterCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Email benzersizlik kontrolü
        if (await unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
            throw new ValidationException("Bu email adresi zaten kullanılmaktadır.");

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Phone = request.Phone,
            Company = request.Company
        };

        await unitOfWork.Users.AddAsync(user, cancellationToken);

        // Kullanıcıya boş sepet oluştur
        var cart = new DomainCart { User = user };
        await unitOfWork.Carts.AddAsync(cart, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();

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
            DateTime.UtcNow.AddHours(1));
    }
}
