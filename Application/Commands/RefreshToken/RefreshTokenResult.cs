namespace BackBase.Application.Commands.RefreshToken;

public record RefreshTokenResult(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
