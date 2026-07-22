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

    /// <summary>Kullanıcıya özel global indirim oranı (yüzde olarak, örn: 10.00 %10 indirim demektir).</summary>
    public decimal GlobalDiscountRate { get; set; } = 0;

    /// <summary>Cari Bakiye (Cari Hesap Bakiyesi).</summary>
    public decimal CurrentBalance { get; set; } = 0;

    public int? SalesRepresentativeId { get; set; }
    public User? SalesRepresentative { get; set; }

    // --- Navigation Properties ---
    public ICollection<Order> Orders { get; set; } = [];
    public Cart? Cart { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<User> Clients { get; set; } = [];
    public ICollection<UserCategoryDiscount> CategoryDiscounts { get; set; } = [];
}
