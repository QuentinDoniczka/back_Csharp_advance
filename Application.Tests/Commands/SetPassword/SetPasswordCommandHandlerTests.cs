namespace BackBase.Application.Tests.Commands.SetPassword;

using BackBase.Application.Commands.SetPassword;
using BackBase.Application.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class SetPasswordCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly SetPasswordCommandHandler _handler;

    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const string ValidPassword = "StrongPass1";

    public SetPasswordCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _handler = new SetPasswordCommandHandler(_identityService);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSetPasswordAsyncWithCorrectArguments()
    {
        // Arrange
        var command = new SetPasswordCommand(ValidUserId, ValidPassword);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _identityService
            .Received(1)
            .SetPasswordAsync(ValidUserId, ValidPassword, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var command = new SetPasswordCommand(ValidUserId, ValidPassword);

        _identityService
            .SetPasswordAsync(ValidUserId, ValidPassword, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("User already has a password"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("User already has a password", exception.Message);
    }
}
