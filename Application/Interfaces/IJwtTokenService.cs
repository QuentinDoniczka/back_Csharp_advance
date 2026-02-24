namespace BackBase.Application.Interfaces;

using System.Security.Claims;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string email);
    (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken();
    string HashToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);
}
