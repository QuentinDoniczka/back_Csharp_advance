namespace BackBase.API.DTOs;

public record LoginResponseDto(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
