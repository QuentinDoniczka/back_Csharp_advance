namespace BackBase.Infrastructure.Repositories;

using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using BackBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _dbContext;

    public UserProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserProfiles
            .AddAsync(profile, cancellationToken)
            .ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        _dbContext.UserProfiles.Update(profile);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
