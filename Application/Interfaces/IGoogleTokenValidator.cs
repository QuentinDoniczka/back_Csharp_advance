namespace BackBase.Application.Interfaces;

using BackBase.Application.DTOs.Output;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
