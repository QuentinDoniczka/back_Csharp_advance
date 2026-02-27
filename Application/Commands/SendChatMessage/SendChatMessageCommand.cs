using BackBase.Application.DTOs.Output;
using MediatR;

namespace BackBase.Application.Commands.SendChatMessage;

public record SendChatMessageCommand(
    Guid SenderUserId,
    string SenderEmail,
    string Message) : IRequest<ChatMessageOutput>;
