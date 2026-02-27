using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BackBase.Infrastructure.Chat;

public sealed class ChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatNotificationService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastToGroupAsync(string groupName, ChatMessageOutput message, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(groupName)
            .SendAsync(ChatConstants.ReceiveMessageMethod, message, cancellationToken)
            .ConfigureAwait(false);
    }
}
