using BackBase.Application.Constants;
using BackBase.Domain.Enums;

namespace BackBase.Application.Helpers;

public static class ChannelNameBuilder
{
    public static string Build(ChannelType channelType, string channelId)
    {
        var prefix = GetPrefix(channelType);
        return $"{prefix}{ChannelConstants.ChannelSeparator}{channelId}";
    }

    private static string GetPrefix(ChannelType channelType)
    {
        return channelType switch
        {
            ChannelType.Global => ChannelConstants.GlobalPrefix,
            ChannelType.Guild => ChannelConstants.GuildPrefix,
            ChannelType.DirectMessage => ChannelConstants.DirectMessagePrefix,
            ChannelType.Party => ChannelConstants.PartyPrefix,
            _ => throw new ArgumentOutOfRangeException(nameof(channelType), channelType, ChannelConstants.InvalidChannelType)
        };
    }
}
