namespace ASK.Domain.Entities;

/// <summary>
/// İletişim formu mesajlarını saklar.
/// Admin panelinden okunabilir/yönetilebilir.
/// </summary>
public class ContactMessage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Admin tarafından okundu mu?</summary>
    public bool IsRead { get; set; }
}
