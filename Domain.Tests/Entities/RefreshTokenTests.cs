namespace BackBase.Domain.Tests.Entities;

using BackBase.Domain.Entities;

public sealed class RefreshTokenTests
{
    private const string ValidTokenHash = "valid-token-hash";
    private static readonly Guid ValidUserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArguments_SetsTokenHash()
    {
        // Arrange & Act
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Assert
        Assert.Equal(ValidTokenHash, token.TokenHash);
    }

    [Fact]
    public void Create_ValidArguments_SetsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var token = RefreshToken.Create(ValidTokenHash, userId, DateTime.UtcNow.AddDays(30));

        // Assert
        Assert.Equal(userId, token.UserId);
    }

    [Fact]
    public void Create_ValidArguments_SetsExpiresAt()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, expiresAt);

        // Assert
        Assert.Equal(expiresAt, token.ExpiresAt);
    }

    [Fact]
    public void Create_ValidArguments_GeneratesNonEmptyId()
    {
        // Arrange & Act
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Assert
        Assert.NotEqual(Guid.Empty, token.Id);
    }

    [Fact]
    public void Create_WithoutExplicitCreatedAt_SetsCreatedAtToApproximatelyUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(token.CreatedAt, before, after);
    }

    [Fact]
    public void Create_WithExplicitCreatedAt_UsesProvidedValue()
    {
        // Arrange
        var explicitCreatedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30), explicitCreatedAt);

        // Assert
        Assert.Equal(explicitCreatedAt, token.CreatedAt);
    }

    [Fact]
    public void Create_ValidArguments_RevokedAtIsNull()
    {
        // Arrange & Act
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Assert
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void Create_ValidArguments_ReplacedByTokenHashIsNull()
    {
        // Arrange & Act
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Assert
        Assert.Null(token.ReplacedByTokenHash);
    }

    [Fact]
    public void IsExpired_TokenNotExpired_ReturnsFalse()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Act
        var isExpired = token.IsExpired;

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void IsExpired_TokenExpired_ReturnsTrue()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddSeconds(-1));

        // Act
        var isExpired = token.IsExpired;

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void IsRevoked_NewToken_ReturnsFalse()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Act
        var isRevoked = token.IsRevoked;

        // Assert
        Assert.False(isRevoked);
    }

    [Fact]
    public void IsRevoked_AfterRevoke_ReturnsTrue()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Act
        token.Revoke();
        var isRevoked = token.IsRevoked;

        // Assert
        Assert.True(isRevoked);
    }

    [Fact]
    public void IsActive_NewNonExpiredToken_ReturnsTrue()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Act
        var isActive = token.IsActive;

        // Assert
        Assert.True(isActive);
    }

    [Fact]
    public void IsActive_RevokedToken_ReturnsFalse()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));
        token.Revoke();

        // Act
        var isActive = token.IsActive;

        // Assert
        Assert.False(isActive);
    }

    [Fact]
    public void IsActive_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddSeconds(-1));

        // Act
        var isActive = token.IsActive;

        // Assert
        Assert.False(isActive);
    }

    [Fact]
    public void Revoke_WithoutArguments_SetsRevokedAtToApproximatelyUtcNow()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));
        var before = DateTime.UtcNow;

        // Act
        token.Revoke();

        // Assert
        var after = DateTime.UtcNow;
        Assert.NotNull(token.RevokedAt);
        Assert.InRange(token.RevokedAt.Value, before, after);
    }

    [Fact]
    public void Revoke_WithExplicitRevokedAt_UsesProvidedValue()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));
        var explicitRevokedAt = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        token.Revoke(revokedAt: explicitRevokedAt);

        // Assert
        Assert.Equal(explicitRevokedAt, token.RevokedAt);
    }

    [Fact]
    public void Revoke_WithReplacedByTokenHash_SetsReplacedByTokenHash()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));
        var replacementHash = "replacement-hash";

        // Act
        token.Revoke(replacedByTokenHash: replacementHash);

        // Assert
        Assert.Equal(replacementHash, token.ReplacedByTokenHash);
    }

    [Fact]
    public void Revoke_WithoutReplacedByTokenHash_LeavesReplacedByTokenHashNull()
    {
        // Arrange
        var token = RefreshToken.Create(ValidTokenHash, ValidUserId, DateTime.UtcNow.AddDays(30));

        // Act
        token.Revoke();

        // Assert
        Assert.Null(token.ReplacedByTokenHash);
    }
}
