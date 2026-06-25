using ASK.Domain.Enums;

namespace ASK.Domain.Entities;

/// <summary>
/// Sistemdeki kullanıcıları temsil eder.
/// Hem müşteri (Customer) hem de yönetici (Admin) bu entity üzerinden yönetilir.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Email benzersiz olmalı, login için kullanılır.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt ile hashlenmiş şifre. Ham şifre asla saklanmaz.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>Pasif kullanıcılar giriş yapamaz (soft ban).</summary>
    public bool IsActive { get; set; } = true;

    // --- Navigation Properties ---
    public ICollection<Order> Orders { get; set; } = [];
    public Cart? Cart { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
