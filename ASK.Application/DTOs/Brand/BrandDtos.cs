namespace ASK.Application.DTOs.Brand;

/// <summary>Marka okuma DTO'su. Frontend Brand interface ile uyumlu.</summary>
public record BrandDto(
    int Id,
    string Name,
    string? LogoUrl,
    string? Website
);

public record CreateBrandDto(
    string Name,
    string? LogoUrl,
    string? Website
);

public record UpdateBrandDto(
    string Name,
    string? LogoUrl,
    string? Website,
    bool IsActive
);
