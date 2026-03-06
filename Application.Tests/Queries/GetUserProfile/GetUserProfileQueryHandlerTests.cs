namespace BackBase.Application.Tests.Queries.GetUserProfile;

using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Queries.GetUserProfile;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using NSubstitute;

public sealed class GetUserProfileQueryHandlerTests
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly GetUserProfileQueryHandler _handler;

    public GetUserProfileQueryHandlerTests()
    {
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _handler = new GetUserProfileQueryHandler(_userProfileRepository);
    }

    [Fact]
    public async Task Handle_ExistingProfile_ReturnsUserProfileOutput()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "AnotherPlayer");
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var query = new GetUserProfileQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("AnotherPlayer", result.DisplayName);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var query = new GetUserProfileQuery(userId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
        Assert.Equal("Profile not found", exception.Message);
    }
}
