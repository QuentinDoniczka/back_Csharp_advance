namespace BackBase.Infrastructure.Repositories;

using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using BackBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public sealed class RevokedTokenRepository : IRevokedTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RevokedTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RevokedTokens
            .AnyAsync(t => t.Jti == jti, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RevokeAsync(RevokedToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.RevokedTokens
            .AddAsync(token, cancellationToken)
            .ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.RevokedTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
