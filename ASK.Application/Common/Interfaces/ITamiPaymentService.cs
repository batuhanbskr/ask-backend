namespace ASK.Application.Common.Interfaces;

public record ThreeDAuthResultDto(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    string? ThreeDSHtmlContent,
    string? OrderId
);

public record Complete3DPaymentResultDto(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    string? OrderId,
    decimal Amount
);

public interface ITamiPaymentService
{
    Task<ThreeDAuthResultDto> Initiate3DPaymentAsync(
        int userId,
        decimal amount,
        string cardHolderName,
        string cardNumber,
        int expireMonth,
        int expireYear,
        string cvv,
        string callbackUrl,
        CancellationToken ct);

    Task<Complete3DPaymentResultDto> Complete3DPaymentAsync(string orderId, CancellationToken ct);
}
