namespace ProcurementApi.DTOs;

public record NotificationDto(
    Guid Id,
    string Event,
    string Message,
    Guid? PurchaseRequestId,
    string? PurchaseRequestNumber,
    bool IsRead,
    DateTime CreatedAt
);

public record UnreadCountDto(int Count);
