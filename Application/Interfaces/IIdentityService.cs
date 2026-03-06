namespace BackBase.Application.Interfaces;

using BackBase.Application.DTOs.Output;

public interface IIdentityService
{
    Task<IdentityUserResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<IdentityUserResult> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<IdentityUserResult?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsBannedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
    Task<ExternalLoginResult> FindOrCreateExternalUserAsync(
        string email,
        string providerName,
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPasswordAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SetPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task ReplaceRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default);
    Task<int> CountUsersInRoleAsync(string role, CancellationToken cancellationToken = default);
}
