namespace BackBase.Application.Commands.Login;

using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserProfileRepository _userProfileRepository;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IUserProfileRepository userProfileRepository)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<AuthTokenResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken).ConfigureAwait(false);

        var profile = await _userProfileRepository.GetByUserIdAsync(user.UserId, cancellationToken).ConfigureAwait(false);
        if (profile is not null && profile.IsDeactivated)
            throw new AuthenticationException(AuthErrorMessages.AccountDeactivated);

        var roles = await _identityService.GetRolesAsync(user.UserId, cancellationToken).ConfigureAwait(false);

        var (accessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user.UserId, user.Email, roles);
        var (refreshToken, _) = _jwtTokenService.GenerateRefreshToken(user.UserId, user.Email);

        return new AuthTokenResult(accessToken, refreshToken, accessExpiresAt);
    }
}
