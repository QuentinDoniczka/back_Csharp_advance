namespace BackBase.Application.Commands.Login;

public record LoginResult(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
