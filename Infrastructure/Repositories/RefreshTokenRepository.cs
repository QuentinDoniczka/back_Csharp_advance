namespace BackBase.Infrastructure.Repositories;

using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using BackBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken?> GetByTokenHashAndUserIdAsync(string tokenHash, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
