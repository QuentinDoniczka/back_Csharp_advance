namespace BackBase.API.DTOs;

public record RefreshTokenResponseDto(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
