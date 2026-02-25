namespace BackBase.Application.Commands.RefreshToken;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokenResult>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRevokedTokenRepository _revokedTokenRepository;
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(
        IJwtTokenService jwtTokenService,
        IRevokedTokenRepository revokedTokenRepository,
        IIdentityService identityService)
    {
        _jwtTokenService = jwtTokenService;
        _revokedTokenRepository = revokedTokenRepository;
        _identityService = identityService;
    }

    public async Task<AuthTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenInfo = _jwtTokenService.ValidateAndExtractRefreshTokenInfo(request.RefreshToken);
        if (tokenInfo is null)
            throw new AuthenticationException("Invalid refresh token");

        if (await _revokedTokenRepository.IsRevokedAsync(tokenInfo.Jti, cancellationToken).ConfigureAwait(false))
            throw new AuthenticationException("Refresh token has been revoked");

        if (await _identityService.IsBannedAsync(tokenInfo.UserId, cancellationToken).ConfigureAwait(false))
            throw new AuthenticationException("User account is suspended");

        var user = await _identityService.FindByIdAsync(tokenInfo.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            throw new AuthenticationException("Invalid refresh token");

        var roles = await _identityService.GetRolesAsync(tokenInfo.UserId, cancellationToken).ConfigureAwait(false);

        var (accessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(tokenInfo.UserId, user.Email, roles);
        var (refreshToken, _) = _jwtTokenService.GenerateRefreshToken(tokenInfo.UserId, user.Email);

        return new AuthTokenResult(accessToken, refreshToken, accessExpiresAt);
    }
}
