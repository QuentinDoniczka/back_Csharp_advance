namespace BackBase.Application.Interfaces;

using BackBase.Application.DTOs.Output;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string email);
    (string Token, DateTime ExpiresAt) GenerateRefreshToken(Guid userId, string email);
    RefreshTokenInfo? ValidateAndExtractRefreshTokenInfo(string refreshToken);
}
