namespace ASK.Domain.Entities;

/// <summary>
/// Tüm entity'lerin türetildiği temel sınıf.
/// Ortak alanları (Id, zaman damgaları) tek yerden yönetir.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
