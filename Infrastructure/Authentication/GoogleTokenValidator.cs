namespace BackBase.Infrastructure.Authentication;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthSettings _googleAuthSettings;

    public GoogleTokenValidator(IOptions<GoogleAuthSettings> googleAuthSettings)
    {
        _googleAuthSettings = googleAuthSettings.Value;
    }

    public async Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleAuthSettings.ClientId]
            };

            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings).ConfigureAwait(false);
        }
        catch (InvalidJwtException)
        {
            throw new AuthenticationException("Invalid Google ID token");
        }

        if (payload.Email is null || !payload.EmailVerified)
        {
            throw new AuthenticationException("Google account email is not verified");
        }

        return new GoogleUserInfo(payload.Email, payload.Subject, payload.Name);
    }
}
