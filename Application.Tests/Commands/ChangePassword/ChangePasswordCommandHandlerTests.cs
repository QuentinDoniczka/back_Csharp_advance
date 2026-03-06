namespace BackBase.Application.Tests.Commands.ChangePassword;

using BackBase.Application.Commands.ChangePassword;
using BackBase.Application.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class ChangePasswordCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _handler = new ChangePasswordCommandHandler(_identityService);
    }

    [Fact]
    public async Task Handle_ValidCommand_CompletesWithoutException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ChangePasswordCommand(userId, "OldPass1!", "NewPass1!");

        // Act & Assert — no exception means success
        await _handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_PropagatesException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ChangePasswordCommand(userId, "WrongOld1!", "NewPass1!");
        _identityService.ChangePasswordAsync(userId, "WrongOld1!", "NewPass1!", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Incorrect password"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Incorrect password", exception.Message);
    }
}
