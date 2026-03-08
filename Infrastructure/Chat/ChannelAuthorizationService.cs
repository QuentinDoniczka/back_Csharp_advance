using BackBase.Application.Constants;
using BackBase.Application.Interfaces;
using BackBase.Domain.Enums;

namespace BackBase.Infrastructure.Chat;

public sealed class ChannelAuthorizationService : IChannelAuthorizationService
{
    public Task<bool> CanUserAccessChannelAsync(Guid userId, ChannelType channelType, string channelId, CancellationToken cancellationToken = default)
    {
        return channelType switch
        {
            ChannelType.Global => Task.FromResult(true),
            ChannelType.Guild or ChannelType.DirectMessage or ChannelType.Party =>
                Task.FromResult(false),
            _ => throw new ArgumentOutOfRangeException(nameof(channelType), channelType, ChannelConstants.InvalidChannelType)
        };
    }
}
