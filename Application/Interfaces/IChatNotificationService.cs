using BackBase.Application.DTOs.Output;

namespace BackBase.Application.Interfaces;

public interface IChatNotificationService
{
    Task BroadcastToGroupAsync(string groupName, ChatMessageOutput message, CancellationToken cancellationToken = default);
}
