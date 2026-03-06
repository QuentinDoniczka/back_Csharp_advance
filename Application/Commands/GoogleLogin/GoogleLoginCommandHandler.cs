namespace BackBase.Application.Commands.GoogleLogin;

using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Constants;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, GoogleLoginResult>
{
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserProfileRepository _userProfileRepository;

    public GoogleLoginCommandHandler(
        IGoogleTokenValidator googleTokenValidator,
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IUserProfileRepository userProfileRepository)
    {
        _googleTokenValidator = googleTokenValidator;
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<GoogleLoginResult> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var googleUser = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken).ConfigureAwait(false);

        var user = await _identityService.FindOrCreateExternalUserAsync(
            googleUser.Email, ExternalProviders.Google, googleUser.GoogleUserId, cancellationToken).ConfigureAwait(false);

        if (await _identityService.IsBannedAsync(user.UserId, cancellationToken).ConfigureAwait(false))
            throw new AuthenticationException(AuthErrorMessages.UserAccountBanned);

        if (user.IsNewAccount)
        {
            var displayName = user.Email.Split('@')[0];
            if (displayName.Length > ProfileConstants.DisplayNameMaxLength)
                displayName = displayName[..ProfileConstants.DisplayNameMaxLength];

            var profile = UserProfile.Create(user.UserId, displayName);
            await _userProfileRepository.AddAsync(profile, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var profile = await _userProfileRepository.GetByUserIdAsync(user.UserId, cancellationToken).ConfigureAwait(false);
            if (profile is not null && profile.IsDeactivated)
                throw new AuthenticationException(AuthErrorMessages.AccountDeactivated);
        }

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
