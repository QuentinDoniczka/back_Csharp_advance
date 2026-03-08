namespace BackBase.Application.DTOs.Output;

public record ChatMessageOutput(
    Guid SenderUserId,
    string SenderEmail,
    string Message,
    DateTime SentAt);
