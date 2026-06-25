namespace ASK.Application.DTOs.Contact;

public record SendContactMessageDto(
    string Name,
    string Email,
    string? Phone,
    string? Company,
    string Subject,
    string Message
);

public record ContactMessageDto(
    int Id,
    string Name,
    string Email,
    string? Phone,
    string? Company,
    string Subject,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);
