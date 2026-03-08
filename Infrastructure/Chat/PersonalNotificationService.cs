using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BackBase.Infrastructure.Chat;

public sealed class PersonalNotificationService : IPersonalNotificationService
{
    private readonly IHubContext<GameHub> _hubContext;

    public PersonalNotificationService(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(Guid userId, NotificationOutput notification, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync(NotificationConstants.ReceiveNotificationMethod, notification, cancellationToken)
            .ConfigureAwait(false);
    }
}
