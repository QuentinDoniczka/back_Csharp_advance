namespace BackBase.Infrastructure.Authentication;

using System.Text;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 30;
    public int RefreshTokenExpirationDays { get; init; } = 30;

    public TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ClockSkew = TimeSpan.Zero
        };
    }
}
