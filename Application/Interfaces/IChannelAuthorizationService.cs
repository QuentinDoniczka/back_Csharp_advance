using BackBase.Domain.Enums;

namespace BackBase.Application.Interfaces;

public interface IChannelAuthorizationService
{
    Task<bool> CanUserAccessChannelAsync(Guid userId, ChannelType channelType, string channelId, CancellationToken cancellationToken = default);
}
