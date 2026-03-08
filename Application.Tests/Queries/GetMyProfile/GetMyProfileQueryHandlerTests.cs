namespace BackBase.Application.Tests.Queries.GetMyProfile;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Queries.GetMyProfile;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using NSubstitute;

public sealed class GetMyProfileQueryHandlerTests
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly GetMyProfileQueryHandler _handler;

    public GetMyProfileQueryHandlerTests()
    {
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _handler = new GetMyProfileQueryHandler(_userProfileRepository);
    }

    [Fact]
    public async Task Handle_ExistingProfile_ReturnsUserProfileOutput()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "PlayerOne", "https://example.com/avatar.png");
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var query = new GetMyProfileQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("PlayerOne", result.DisplayName);
        Assert.Equal("https://example.com/avatar.png", result.AvatarUrl);
        Assert.False(result.IsDeactivated);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var query = new GetMyProfileQuery(userId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
        Assert.Equal("Profile not found", exception.Message);
    }
}
