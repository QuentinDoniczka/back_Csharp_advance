namespace BackBase.Domain.Interfaces;

using BackBase.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAndUserIdAsync(string tokenHash, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
