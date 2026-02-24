namespace BackBase.Application.Commands.Login;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthTokenResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken).ConfigureAwait(false);

        var (accessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user.UserId, user.Email);
        var (rawRefreshToken, refreshHash, refreshExpiresAt) = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(refreshHash, user.UserId, refreshExpiresAt);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthTokenResult(accessToken, rawRefreshToken, accessExpiresAt);
    }
}
