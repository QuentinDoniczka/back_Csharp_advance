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
}
