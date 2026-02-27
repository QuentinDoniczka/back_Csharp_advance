namespace BackBase.Application.Constants;

public static class ChatConstants
{
    public const int MaxMessageLength = 500;
    public const string GlobalChatGroup = "GlobalChat";
    public static readonly string MessageTooLong = $"Message must not exceed {MaxMessageLength} characters.";
    public const string MessageEmpty = "Message must not be empty.";
    public const string ReceiveMessageMethod = "ReceiveMessage";
    public const string HubPath = "/chat";
}
