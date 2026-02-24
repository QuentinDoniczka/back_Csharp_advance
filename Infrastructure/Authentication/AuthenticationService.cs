namespace BackBase.Infrastructure.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BackBase.Application.Commands.Login;
using BackBase.Application.Commands.RefreshToken;
using BackBase.Application.Commands.Register;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Infrastructure.Data;
using DomainRefreshToken = BackBase.Domain.Models.RefreshToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly AppDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationService(
        UserManager<IdentityUser<Guid>> userManager,
        JwtTokenService jwtTokenService,
        AppDbContext dbContext,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<RegisterResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new IdentityUser<Guid>
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException(errors);
        }

        return new RegisterResult(user.Id, user.Email!);
    }

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthenticationException("Invalid credentials");
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false);
        if (!isValidPassword)
        {
            throw new AuthenticationException("Invalid credentials");
        }

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user.Id, email);
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var tokenHash = JwtTokenService.HashToken(rawRefreshToken);

        var refreshToken = DomainRefreshToken.Create(
            tokenHash,
            user.Id,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new LoginResult(accessToken, rawRefreshToken, expiresAt);
    }

    public async Task<RefreshTokenResult> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(accessToken);
        if (principal is null)
        {
            throw new AuthenticationException("Invalid access token");
        }

        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new AuthenticationException("Invalid access token");
        }

        var tokenHash = JwtTokenService.HashToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new AuthenticationException("Invalid refresh token");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthenticationException("User not found");
        }

        var (newAccessToken, newExpiresAt) = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!);
        var newRawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newTokenHash = JwtTokenService.HashToken(newRawRefreshToken);

        storedToken.Revoke(newTokenHash);

        var newRefreshToken = DomainRefreshToken.Create(
            newTokenHash,
            user.Id,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RefreshTokenResult(newAccessToken, newRawRefreshToken, newExpiresAt);
    }
}
