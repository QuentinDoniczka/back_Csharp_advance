namespace BackBase.Infrastructure.Authentication;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public IdentityService(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IdentityUserResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new IdentityUser<Guid> { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, password).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException(errors);
        }

        return new IdentityUserResult(user.Id, user.Email!);
    }

    public async Task<IdentityUserResult> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthenticationException("Invalid credentials");
        }

        var isValid = await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false);
        if (!isValid)
        {
            throw new AuthenticationException("Invalid credentials");
        }

        return new IdentityUserResult(user.Id, user.Email!);
    }

    public async Task<IdentityUserResult?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        return user is null ? null : new IdentityUserResult(user.Id, user.Email!);
    }
}
