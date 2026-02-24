namespace BackBase.Application.Commands.RefreshToken;

using System.Security.Claims;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Interfaces;
using MediatR;
using RefreshTokenEntity = BackBase.Domain.Entities.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokenResult>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityService identityService)
    {
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _identityService = identityService;
    }

    public async Task<AuthTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            throw new AuthenticationException("Invalid access token");
        }

        var userIdClaim = principal.FindFirst("sub")
            ?? principal.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new AuthenticationException("Invalid access token");
        }

        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAndUserIdAsync(tokenHash, userId, cancellationToken).ConfigureAwait(false);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new AuthenticationException("Invalid refresh token");
        }

        var user = await _identityService.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthenticationException("Invalid refresh token");
        }

        var (newAccessToken, newAccessExpiresAt) = _jwtTokenService.GenerateAccessToken(userId, user.Email);
        var (newRawRefresh, newRefreshHash, newRefreshExpiresAt) = _jwtTokenService.GenerateRefreshToken();

        storedToken.Revoke(newRefreshHash);

        var newRefreshToken = RefreshTokenEntity.Create(newRefreshHash, userId, newRefreshExpiresAt);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken).ConfigureAwait(false);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthTokenResult(newAccessToken, newRawRefresh, newAccessExpiresAt);
    }
}
