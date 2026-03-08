namespace BackBase.Domain.Entities;

public sealed class RevokedToken
{
    public Guid Id { get; private set; }
    public string Jti { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime RevokedAt { get; private set; }

    private RevokedToken() { }

    public static RevokedToken Create(string jti, Guid userId, DateTime expiresAt, DateTime? revokedAt = null)
    {
        return new RevokedToken
        {
            Id = Guid.NewGuid(),
            Jti = jti,
            UserId = userId,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt ?? DateTime.UtcNow
        };
    }
}
