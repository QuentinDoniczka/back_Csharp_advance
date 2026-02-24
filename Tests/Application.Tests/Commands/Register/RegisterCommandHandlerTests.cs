namespace BackBase.Application.Tests.Commands.Register;

using BackBase.Application.Commands.Register;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class RegisterCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _handler = new RegisterCommandHandler(_identityService);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRegisterResultWithCorrectUserId()
    {
        // Arrange
        var email = "player@example.com";
        var password = "StrongPass1";
        var command = new RegisterCommand(email, password);
        var expectedUserId = Guid.NewGuid();

        _identityService
            .RegisterAsync(email, password, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(expectedUserId, email));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUserId, result.UserId);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRegisterResultWithCorrectEmail()
    {
        // Arrange
        var email = "hero@example.com";
        var password = "StrongPass1";
        var command = new RegisterCommand(email, password);
        var userId = Guid.NewGuid();

        _identityService
            .RegisterAsync(email, password, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(userId, email));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRegisterAsyncWithCorrectArguments()
    {
        // Arrange
        var email = "delegate@example.com";
        var password = "Password123";
        var command = new RegisterCommand(email, password);

        _identityService
            .RegisterAsync(email, password, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(Guid.NewGuid(), email));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _identityService
            .Received(1)
            .RegisterAsync(email, password, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var email = "existing@example.com";
        var password = "Password123";
        var command = new RegisterCommand(email, password);

        _identityService
            .RegisterAsync(email, password, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("User already exists"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("User already exists", exception.Message);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToCancellationToken()
    {
        // Arrange
        var email = "cancel@example.com";
        var password = "Password123";
        var command = new RegisterCommand(email, password);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _identityService
            .RegisterAsync(email, password, token)
            .Returns(new IdentityUserResult(Guid.NewGuid(), email));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _identityService
            .Received(1)
            .RegisterAsync(email, password, token);
    }
}
