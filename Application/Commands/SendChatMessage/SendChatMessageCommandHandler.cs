using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using MediatR;

namespace BackBase.Application.Commands.SendChatMessage;

public sealed class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatMessageOutput>
{
    private readonly IChatNotificationService _chatNotificationService;

    public SendChatMessageCommandHandler(IChatNotificationService chatNotificationService)
    {
        _chatNotificationService = chatNotificationService;
    }

    public async Task<ChatMessageOutput> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        var message = new ChatMessageOutput(
            request.SenderUserId,
            request.SenderEmail,
            request.Message,
            DateTime.UtcNow);

        await _chatNotificationService.BroadcastToGroupAsync(
            ChatConstants.GlobalChatGroup,
            message,
            cancellationToken).ConfigureAwait(false);

        return message;
    }
}
