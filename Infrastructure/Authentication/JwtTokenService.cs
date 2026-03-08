namespace BackBase.Infrastructure.Authentication;

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtTokenService : IJwtTokenService
{
    private const string TokenTypeClaim = "token_type";
    private const string RefreshTokenType = "refresh";

    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string email, IReadOnlyList<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));
        return GenerateToken(userId, email, expiresAt, roleClaims);
    }

    public (string Token, DateTime ExpiresAt) GenerateRefreshToken(Guid userId, string email)
    {
        var expiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        return GenerateToken(userId, email, expiresAt, [new Claim(TokenTypeClaim, RefreshTokenType)]);
    }

    public RefreshTokenInfo? ValidateAndExtractRefreshTokenInfo(string refreshToken)
    {
        var tokenValidationParameters = _jwtSettings.CreateTokenValidationParameters();

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(refreshToken, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var tokenTypeClaim = principal.FindFirst(TokenTypeClaim)?.Value;
            if (tokenTypeClaim != RefreshTokenType)
                return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var jtiClaim = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId) ||
                jtiClaim is null || expClaim is null)
            {
                return null;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim, CultureInfo.InvariantCulture)).UtcDateTime;
            return new RefreshTokenInfo(userId, jtiClaim, expiresAt);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    private (string Token, DateTime ExpiresAt) GenerateToken(Guid userId, string email, DateTime expiresAt, IEnumerable<Claim>? additionalClaims = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (additionalClaims is not null)
            claims.AddRange(additionalClaims);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
