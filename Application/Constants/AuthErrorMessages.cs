namespace BackBase.Application.Constants;

public static class AuthErrorMessages
{
    public const string InvalidRefreshToken = "Invalid refresh token";
    public const string RefreshTokenRevoked = "Refresh token has been revoked";
    public const string UserAccountBanned = "User account is banned";
    public const string UserNotFound = "User not found";
    public const string InvalidCredentials = "Invalid credentials";
    public const string ExternalLoginPasswordRequired = "This account uses external login. Please set a password first or log in with Google.";
    public const string UserAlreadyHasPassword = "User already has a password";
}
