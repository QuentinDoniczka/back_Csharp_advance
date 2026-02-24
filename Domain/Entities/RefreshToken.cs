namespace BackBase.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public static RefreshToken Create(string tokenHash, Guid userId, DateTime expiresAt, DateTime? createdAt = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            UserId = userId,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    public void Revoke(string? replacedByTokenHash = null, DateTime? revokedAt = null)
    {
        RevokedAt = revokedAt ?? DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
