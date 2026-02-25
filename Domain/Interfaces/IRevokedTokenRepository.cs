namespace BackBase.Domain.Interfaces;

using BackBase.Domain.Entities;

public interface IRevokedTokenRepository
{
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
    Task RevokeAsync(RevokedToken token, CancellationToken cancellationToken = default);
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
