namespace BackBase.Application.Constants;

public static class ChatConstants
{
    public const int MaxMessageLength = 500;
    public static readonly string MessageTooLong = $"Message must not exceed {MaxMessageLength} characters.";
    public const string MessageEmpty = "Message must not be empty.";
    public const string ReceiveMessageMethod = "ReceiveMessage";
    public const string SendMessageMethod = "SendMessage";
    public const string JoinSalonMethod = "JoinSalon";
    public const string LeaveSalonMethod = "LeaveSalon";
    public const string SalonNameEmpty = "Salon name must not be empty.";
    public const int SalonNameMaxLength = 50;
    public static readonly string SalonNameTooLong = $"Salon name must not exceed {SalonNameMaxLength} characters.";
    public const string HubPath = "/game";

    public const string UserIdentityNotFound = "User identity not found.";
    public const string InvalidUserIdFormat = "User ID claim is not a valid GUID.";
    public const string UserEmailNotFound = "User email not found.";
}
