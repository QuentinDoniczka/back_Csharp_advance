namespace BackBase.Application.Tests.Queries.GetUserRole;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Application.Queries.GetUserRole;
using BackBase.Domain.Constants;
using NSubstitute;

public sealed class GetUserRoleQueryHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly GetUserRoleQueryHandler _handler;

    public GetUserRoleQueryHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _handler = new GetUserRoleQueryHandler(_identityService);
    }

    [Fact]
    public async Task Handle_UserWithRoles_ReturnsFirstRole()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        _identityService.FindByIdAsync(targetUserId, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(targetUserId, "player@example.com"));
        IReadOnlyList<string> roles = new List<string> { AppRoles.Admin }.AsReadOnly();
        _identityService.GetRolesAsync(targetUserId, Arg.Any<CancellationToken>()).Returns(roles);
        var query = new GetUserRoleQuery(targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(targetUserId, result.UserId);
        Assert.Equal(AppRoles.Admin, result.Role);
    }

    [Fact]
    public async Task Handle_UserWithNoRoles_FallsBackToMember()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        _identityService.FindByIdAsync(targetUserId, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(targetUserId, "player@example.com"));
        IReadOnlyList<string> emptyRoles = new List<string>().AsReadOnly();
        _identityService.GetRolesAsync(targetUserId, Arg.Any<CancellationToken>()).Returns(emptyRoles);
        var query = new GetUserRoleQuery(targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(AppRoles.Member, result.Role);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        _identityService.FindByIdAsync(targetUserId, Arg.Any<CancellationToken>())
            .Returns((IdentityUserResult?)null);
        var query = new GetUserRoleQuery(targetUserId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
        Assert.Equal("User not found", exception.Message);
    }
}
