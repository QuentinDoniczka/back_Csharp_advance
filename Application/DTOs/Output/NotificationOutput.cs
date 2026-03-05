using BackBase.Domain.Enums;

namespace BackBase.Application.DTOs.Output;

public record NotificationOutput(
    NotificationType Type,
    Guid? ReferenceId,
    int? Count,
    DateTime OccurredAt);
