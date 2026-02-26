namespace BackBase.Application.Commands.Logout;

using BackBase.Application.Constants;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRevokedTokenRepository _revokedTokenRepository;

    public LogoutCommandHandler(
        IJwtTokenService jwtTokenService,
        IRevokedTokenRepository revokedTokenRepository)
    {
        _jwtTokenService = jwtTokenService;
        _revokedTokenRepository = revokedTokenRepository;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenInfo = _jwtTokenService.ValidateAndExtractRefreshTokenInfo(request.RefreshToken);
        if (tokenInfo is null)
            throw new AuthenticationException(AuthErrorMessages.InvalidRefreshToken);

        if (await _revokedTokenRepository.IsRevokedAsync(tokenInfo.Jti, cancellationToken).ConfigureAwait(false))
            return;

        var revokedToken = RevokedToken.Create(tokenInfo.Jti, tokenInfo.UserId, tokenInfo.ExpiresAt);
        await _revokedTokenRepository.RevokeAsync(revokedToken, cancellationToken).ConfigureAwait(false);
    }
}
