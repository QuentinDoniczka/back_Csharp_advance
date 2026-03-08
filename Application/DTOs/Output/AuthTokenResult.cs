namespace BackBase.Application.DTOs.Output;

public record AuthTokenResult(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
