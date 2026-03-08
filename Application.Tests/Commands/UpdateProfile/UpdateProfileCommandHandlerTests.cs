namespace BackBase.Application.Tests.Commands.UpdateProfile;

using BackBase.Application.Commands.UpdateProfile;
using BackBase.Application.Exceptions;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using NSubstitute;

public sealed class UpdateProfileCommandHandlerTests
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _handler = new UpdateProfileCommandHandler(_userProfileRepository);
    }

    [Fact]
    public async Task Handle_ExistingProfile_ReturnsUpdatedProfileOutput()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "OldName", "https://example.com/old.png");
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = new UpdateProfileCommand(userId, "NewName", "https://example.com/new.png");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("NewName", result.DisplayName);
        Assert.Equal("https://example.com/new.png", result.AvatarUrl);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var command = new UpdateProfileCommand(userId, "NewName", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Profile not found", exception.Message);
    }
}
