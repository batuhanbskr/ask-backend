namespace ASK.Application.DTOs.Auth;

public record RegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    string? Company
);

public record LoginDto(
    string Email,
    string Password
);

public record RefreshTokenRequestDto(
    string RefreshToken
);

public record SalesRepresentativeDto(
    string FirstName,
    string LastName,
    string Email,
    string? Phone
);

public record AuthResponseDto(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    SalesRepresentativeDto? SalesRepresentative = null
);
