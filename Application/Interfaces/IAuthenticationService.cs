namespace BackBase.Application.Interfaces;

using BackBase.Application.Commands.Login;
using BackBase.Application.Commands.RefreshToken;
using BackBase.Application.Commands.Register;

public interface IAuthenticationService
{
    Task<RegisterResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<RefreshTokenResult> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);
}
