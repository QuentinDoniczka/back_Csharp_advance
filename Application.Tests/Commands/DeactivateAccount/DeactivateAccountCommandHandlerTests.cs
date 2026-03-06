namespace BackBase.Application.Tests.Commands.DeactivateAccount;

using BackBase.Application.Commands.DeactivateAccount;
using BackBase.Application.Exceptions;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using NSubstitute;

public sealed class DeactivateAccountCommandHandlerTests
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly DeactivateAccountCommandHandler _handler;

    public DeactivateAccountCommandHandlerTests()
    {
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _handler = new DeactivateAccountCommandHandler(_userProfileRepository);
    }

    [Fact]
    public async Task Handle_ActiveProfile_DeactivatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "PlayerOne");
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = new DeactivateAccountCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(profile.IsDeactivated);
    }

    [Fact]
    public async Task Handle_AlreadyDeactivated_ThrowsConflictException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "PlayerOne");
        profile.Deactivate();
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = new DeactivateAccountCommand(userId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Account is already deactivated", exception.Message);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userProfileRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var command = new DeactivateAccountCommand(userId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Profile not found", exception.Message);
    }
}
