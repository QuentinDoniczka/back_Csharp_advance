namespace BackBase.Domain.Tests.Entities;

using BackBase.Domain.Entities;

public sealed class RevokedTokenTests
{
    private const string ValidJti = "valid-jti-value";
    private static readonly Guid ValidUserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArguments_SetsJti()
    {
        var token = RevokedToken.Create(ValidJti, ValidUserId, DateTime.UtcNow.AddDays(30));

        Assert.Equal(ValidJti, token.Jti);
    }

    [Fact]
    public void Create_ValidArguments_SetsUserId()
    {
        var userId = Guid.NewGuid();

        var token = RevokedToken.Create(ValidJti, userId, DateTime.UtcNow.AddDays(30));

        Assert.Equal(userId, token.UserId);
    }

    [Fact]
    public void Create_ValidArguments_SetsExpiresAt()
    {
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var token = RevokedToken.Create(ValidJti, ValidUserId, expiresAt);

        Assert.Equal(expiresAt, token.ExpiresAt);
    }

    [Fact]
    public void Create_ValidArguments_GeneratesNonEmptyId()
    {
        var token = RevokedToken.Create(ValidJti, ValidUserId, DateTime.UtcNow.AddDays(30));

        Assert.NotEqual(Guid.Empty, token.Id);
    }

    [Fact]
    public void Create_WithoutExplicitRevokedAt_SetsRevokedAtToApproximatelyUtcNow()
    {
        var before = DateTime.UtcNow;

        var token = RevokedToken.Create(ValidJti, ValidUserId, DateTime.UtcNow.AddDays(30));

        var after = DateTime.UtcNow;
        Assert.InRange(token.RevokedAt, before, after);
    }

    [Fact]
    public void Create_WithExplicitRevokedAt_UsesProvidedValue()
    {
        var explicitRevokedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var token = RevokedToken.Create(ValidJti, ValidUserId, DateTime.UtcNow.AddDays(30), explicitRevokedAt);

        Assert.Equal(explicitRevokedAt, token.RevokedAt);
    }

    [Fact]
    public void Create_TwoTokens_GenerateDifferentIds()
    {
        var token1 = RevokedToken.Create(ValidJti, ValidUserId, DateTime.UtcNow.AddDays(30));
        var token2 = RevokedToken.Create("other-jti", ValidUserId, DateTime.UtcNow.AddDays(30));

        Assert.NotEqual(token1.Id, token2.Id);
    }
}
