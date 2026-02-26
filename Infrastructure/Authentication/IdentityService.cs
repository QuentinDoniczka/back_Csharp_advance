namespace BackBase.Infrastructure.Authentication;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IdentityUserResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
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

    public async Task<bool> IsBannedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return false;
        return user.BannedUntil.HasValue && user.BannedUntil.Value > DateTime.UtcNow;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            throw new AuthenticationException("User not found");

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return roles.ToList().AsReadOnly();
    }

    public async Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            throw new AuthenticationException("User not found");

        var result = await _userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
        if (!result.Succeeded)
            throw new AuthenticationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<IdentityUserResult> FindOrCreateExternalUserAsync(
        string email,
        string providerName,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);

        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await _userManager.CreateAsync(user).ConfigureAwait(false);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new AuthenticationException(errors);
            }
        }
        else if (!user.EmailConfirmed)
        {
            // Auto-linking is safe: the external provider (Google) has already verified email ownership
            // via the EmailVerified check in GoogleTokenValidator. Mark the existing account as confirmed.
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user).ConfigureAwait(false);
        }

        var logins = await _userManager.GetLoginsAsync(user).ConfigureAwait(false);
        var isLinked = logins.Any(l => l.LoginProvider == providerName);

        if (!isLinked)
        {
            var loginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(providerName, providerUserId, providerName)).ConfigureAwait(false);

            if (!loginResult.Succeeded)
            {
                var errors = string.Join(", ", loginResult.Errors.Select(e => e.Description));
                throw new AuthenticationException(errors);
            }
        }

        return new IdentityUserResult(user.Id, user.Email!);
    }
}
