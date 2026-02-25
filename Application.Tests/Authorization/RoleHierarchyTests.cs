namespace BackBase.Application.Tests.Authorization;

using BackBase.Application.Authorization;
using BackBase.Application.Constants;

public sealed class RoleHierarchyTests
{
    [Fact]
    public void TryGetLevel_Player_ReturnsPlayerLevel()
    {
        // Arrange
        var roleName = AppRoles.Player;

        // Act
        var found = RoleHierarchy.TryGetLevel(roleName, out var level);

        // Assert
        Assert.True(found);
        Assert.Equal(RoleLevel.Player, level);
    }

    [Fact]
    public void TryGetLevel_Admin_ReturnsAdminLevel()
    {
        // Arrange
        var roleName = AppRoles.Admin;

        // Act
        var found = RoleHierarchy.TryGetLevel(roleName, out var level);

        // Assert
        Assert.True(found);
        Assert.Equal(RoleLevel.Admin, level);
    }

    [Fact]
    public void TryGetLevel_SuperAdmin_ReturnsSuperAdminLevel()
    {
        // Arrange
        var roleName = AppRoles.SuperAdmin;

        // Act
        var found = RoleHierarchy.TryGetLevel(roleName, out var level);

        // Assert
        Assert.True(found);
        Assert.Equal(RoleLevel.SuperAdmin, level);
    }

    [Fact]
    public void TryGetLevel_CaseInsensitive_ReturnsCorrectLevel()
    {
        // Arrange
        var roleName = "admin";

        // Act
        var found = RoleHierarchy.TryGetLevel(roleName, out var level);

        // Assert
        Assert.True(found);
        Assert.Equal(RoleLevel.Admin, level);
    }

    [Fact]
    public void TryGetLevel_UnknownRole_ReturnsFalse()
    {
        // Arrange
        var roleName = "Moderator";

        // Act
        var found = RoleHierarchy.TryGetLevel(roleName, out _);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void AllAppRoles_ExistInRoleHierarchy()
    {
        // Arrange & Act & Assert
        foreach (var role in AppRoles.All)
        {
            var found = RoleHierarchy.TryGetLevel(role, out _);
            Assert.True(found, $"AppRoles.All contains '{role}' but RoleHierarchy does not recognize it.");
        }
    }

    [Fact]
    public void AllRoleLevels_HaveCorrespondingAppRole()
    {
        // Arrange
        var allRoleLevels = Enum.GetValues<RoleLevel>();

        // Act & Assert
        foreach (var level in allRoleLevels)
        {
            var hasMatch = AppRoles.All.Any(role =>
                RoleHierarchy.TryGetLevel(role, out var mappedLevel) && mappedLevel == level);

            Assert.True(hasMatch, $"RoleLevel.{level} has no corresponding entry in AppRoles.All.");
        }
    }
}
