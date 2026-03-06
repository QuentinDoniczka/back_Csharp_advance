namespace BackBase.Domain.Tests.Entities;

using BackBase.Domain.Entities;

public sealed class UserProfileTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const string ValidDisplayName = "PlayerOne";
    private const string ValidAvatarUrl = "https://example.com/avatar.png";

    [Fact]
    public void Create_ValidParameters_ReturnsProfileWithCorrectProperties()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var profile = UserProfile.Create(ValidUserId, ValidDisplayName, ValidAvatarUrl);
        var after = DateTime.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(ValidUserId, profile.UserId);
        Assert.Equal(ValidDisplayName, profile.DisplayName);
        Assert.Equal(ValidAvatarUrl, profile.AvatarUrl);
        Assert.False(profile.IsDeactivated);
        Assert.Null(profile.DeactivatedAt);
        Assert.InRange(profile.CreatedAt, before, after);
        Assert.InRange(profile.UpdatedAt, before, after);
    }

    [Fact]
    public void Create_WithoutAvatarUrl_AvatarUrlIsNull()
    {
        // Arrange & Act
        var profile = UserProfile.Create(ValidUserId, ValidDisplayName);

        // Assert
        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public void Update_ValidParameters_UpdatesDisplayNameAndAvatarUrl()
    {
        // Arrange
        var profile = UserProfile.Create(ValidUserId, ValidDisplayName, ValidAvatarUrl);
        var originalUpdatedAt = profile.UpdatedAt;
        var newDisplayName = "UpdatedPlayer";
        var newAvatarUrl = "https://example.com/new-avatar.png";

        // Act
        profile.Update(newDisplayName, newAvatarUrl);

        // Assert
        Assert.Equal(newDisplayName, profile.DisplayName);
        Assert.Equal(newAvatarUrl, profile.AvatarUrl);
        Assert.True(profile.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void Deactivate_ActiveProfile_SetsDeactivatedAtAndIsDeactivated()
    {
        // Arrange
        var profile = UserProfile.Create(ValidUserId, ValidDisplayName);

        // Act
        var before = DateTime.UtcNow;
        profile.Deactivate();
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(profile.IsDeactivated);
        Assert.NotNull(profile.DeactivatedAt);
        Assert.InRange(profile.DeactivatedAt.Value, before, after);
    }

    [Fact]
    public void Deactivate_AlreadyDeactivated_ThrowsInvalidOperationException()
    {
        // Arrange
        var profile = UserProfile.Create(ValidUserId, ValidDisplayName);
        profile.Deactivate();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => profile.Deactivate());
        Assert.Equal("Account is already deactivated.", exception.Message);
    }

    [Fact]
    public void Reactivate_DeactivatedProfile_ClearsDeactivatedAt()
    {
        // Arrange
        var profile = UserProfile.Create(ValidUserId, ValidDisplayName);
        profile.Deactivate();

        // Act
        profile.Reactivate();

        // Assert
        Assert.False(profile.IsDeactivated);
        Assert.Null(profile.DeactivatedAt);
    }

    [Fact]
    public void Reactivate_ActiveProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profile = UserProfile.Create(ValidUserId, ValidDisplayName);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => profile.Reactivate());
        Assert.Equal("Account is not deactivated.", exception.Message);
    }
}
