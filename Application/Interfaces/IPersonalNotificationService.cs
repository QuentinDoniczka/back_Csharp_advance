using BackBase.Application.DTOs.Output;

namespace BackBase.Application.Interfaces;

public interface IPersonalNotificationService
{
    Task SendToUserAsync(Guid userId, NotificationOutput notification, CancellationToken cancellationToken = default);
}
