namespace BackBase.Application.Constants;

public static class ChannelConstants
{
    public const string GlobalPrefix = "global";
    public const string GuildPrefix = "guild";
    public const string DirectMessagePrefix = "dm";
    public const string PartyPrefix = "party";

    public const char ChannelSeparator = ':';

    public const int ChannelIdMaxLength = 50;
    public const int ChannelNameMaxLength = 60;

    public const string ChannelIdEmpty = "Channel ID must not be empty.";
    public static readonly string ChannelIdTooLong = $"Channel ID must not exceed {ChannelIdMaxLength} characters.";
    public const string ChannelNameEmpty = "Channel name must not be empty.";
    public static readonly string ChannelNameTooLong = $"Channel name must not exceed {ChannelNameMaxLength} characters.";
    public const string InvalidChannelType = "Invalid channel type.";
    public const string ChannelAccessDenied = "You do not have access to this channel.";

    public const string JoinChannelMethod = "JoinChannel";
    public const string LeaveChannelMethod = "LeaveChannel";
}
