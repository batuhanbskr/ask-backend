namespace ASK.Application.DTOs.Category;

/// <summary>Kategori okuma DTO'su. Frontend Category interface ile uyumlu.</summary>
public record CategoryDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    int ProductCount,
    int? ParentCategoryId
);

public record CreateCategoryDto(
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    int? ParentCategoryId
);

public record UpdateCategoryDto(
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    bool IsActive,
    int? ParentCategoryId
);
