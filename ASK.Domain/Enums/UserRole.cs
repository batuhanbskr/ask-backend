namespace ASK.Domain.Enums;

/// <summary>
/// Kullanıcı rol tanımları.
/// Customer: Normal B2B müşteri.
/// Admin: Tam yetkili sistem yöneticisi.
/// </summary>
public enum UserRole
{
    Customer = 0,
    Admin = 1,
    SuperAdmin = 2
}
