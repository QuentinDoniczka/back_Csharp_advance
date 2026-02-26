namespace BackBase.Application.Commands.GoogleLogin;

using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Constants;
using MediatR;

public sealed class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, GoogleLoginResult>
{
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;

    public GoogleLoginCommandHandler(
        IGoogleTokenValidator googleTokenValidator,
        IIdentityService identityService,
        IJwtTokenService jwtTokenService)
    {
        _googleTokenValidator = googleTokenValidator;
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<GoogleLoginResult> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var googleUser = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken).ConfigureAwait(false);

        var user = await _identityService.FindOrCreateExternalUserAsync(
            googleUser.Email, ExternalProviders.Google, googleUser.GoogleUserId, cancellationToken).ConfigureAwait(false);

        if (await _identityService.IsBannedAsync(user.UserId, cancellationToken).ConfigureAwait(false))
            throw new AuthenticationException(AuthErrorMessages.UserAccountBanned);

        var roles = await _identityService.GetRolesAsync(user.UserId, cancellationToken).ConfigureAwait(false);

        if (roles.Count == 0)
        {
            await _identityService.AssignRoleAsync(user.UserId, AppRoles.Member, cancellationToken).ConfigureAwait(false);
            roles = [AppRoles.Member];
        }

        var (accessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user.UserId, user.Email, roles);
        var (refreshToken, _) = _jwtTokenService.GenerateRefreshToken(user.UserId, user.Email);

        var tokens = new AuthTokenResult(accessToken, refreshToken, accessExpiresAt);
        return new GoogleLoginResult(tokens, user.IsNewAccount);
    }
}
