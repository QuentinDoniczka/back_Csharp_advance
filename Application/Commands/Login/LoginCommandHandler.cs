namespace BackBase.Application.Commands.Login;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using MediatR;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthTokenResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken).ConfigureAwait(false);
        var roles = await _identityService.GetRolesAsync(user.UserId, cancellationToken).ConfigureAwait(false);

        var (accessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user.UserId, user.Email, roles);
        var (refreshToken, _) = _jwtTokenService.GenerateRefreshToken(user.UserId, user.Email);

        return new AuthTokenResult(accessToken, refreshToken, accessExpiresAt);
    }
}
