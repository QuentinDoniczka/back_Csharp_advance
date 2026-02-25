namespace BackBase.Application.Tests.Authorization;

using System.Security.Claims;
using BackBase.Application.Authorization;
using BackBase.Application.Constants;
using BackBase.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;

public sealed class MinimumRoleHandlerTests
{
    private readonly MinimumRoleHandler _handler = new();

    private static AuthorizationHandlerContext CreateContext(
        MinimumRoleRequirement requirement,
        params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            resource: null);
    }

    [Fact]
    public async Task HandleRequirement_UserWithSuperAdmin_PassesAdminRequirement()
    {
        // Arrange
        var requirement = new MinimumRoleRequirement(RoleLevel.Admin);
        var context = CreateContext(requirement, AppRoles.SuperAdmin);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirement_UserWithAdmin_PassesAdminRequirement()
    {
        // Arrange
        var requirement = new MinimumRoleRequirement(RoleLevel.Admin);
        var context = CreateContext(requirement, AppRoles.Admin);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirement_UserWithPlayer_FailsAdminRequirement()
    {
        // Arrange
        var requirement = new MinimumRoleRequirement(RoleLevel.Admin);
        var context = CreateContext(requirement, AppRoles.Player);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirement_UserWithNoRoles_FailsAnyRequirement()
    {
        // Arrange
        var requirement = new MinimumRoleRequirement(RoleLevel.Player);
        var context = CreateContext(requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirement_UserWithMultipleRoles_UsesHighestRole()
    {
        // Arrange
        var requirement = new MinimumRoleRequirement(RoleLevel.Admin);
        var context = CreateContext(requirement, AppRoles.Player, AppRoles.Admin);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirement_UserWithUnknownRole_SkipsIt()
    {
        // Arrange
        var requirement = new MinimumRoleRequirement(RoleLevel.Admin);
        var context = CreateContext(requirement, "Moderator");

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
