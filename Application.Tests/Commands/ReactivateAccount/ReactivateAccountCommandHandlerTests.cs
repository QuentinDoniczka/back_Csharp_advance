namespace BackBase.Application.Tests.Commands.ReactivateAccount;

using BackBase.Application.Commands.ReactivateAccount;
using BackBase.Application.Exceptions;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using NSubstitute;

public sealed class ReactivateAccountCommandHandlerTests
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ReactivateAccountCommandHandler _handler;

    public ReactivateAccountCommandHandlerTests()
    {
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _handler = new ReactivateAccountCommandHandler(_userProfileRepository);
    }

    [Fact]
    public async Task Handle_DeactivatedProfile_ReactivatesSuccessfully()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var profile = UserProfile.Create(targetUserId, "PlayerOne");
        profile.Deactivate();
        _userProfileRepository.GetByUserIdAsync(targetUserId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = new ReactivateAccountCommand(targetUserId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(profile.IsDeactivated);
    }

    [Fact]
    public async Task Handle_ActiveProfile_ThrowsConflictException()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var profile = UserProfile.Create(targetUserId, "PlayerOne");
        _userProfileRepository.GetByUserIdAsync(targetUserId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = new ReactivateAccountCommand(targetUserId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Account is not deactivated", exception.Message);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        _userProfileRepository.GetByUserIdAsync(targetUserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var command = new ReactivateAccountCommand(targetUserId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Profile not found", exception.Message);
    }
}
