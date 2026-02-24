namespace BackBase.Application.Tests.Commands.Register;

using BackBase.Application.Commands.Register;
using BackBase.Application.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class RegisterCommandHandlerTests
{
    private readonly IAuthenticationService _authenticationService;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _authenticationService = Substitute.For<IAuthenticationService>();
        _handler = new RegisterCommandHandler(_authenticationService);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRegisterResult()
    {
        // Arrange
        var email = "player@example.com";
        var password = "StrongPass1";
        var command = new RegisterCommand(email, password);
        var expectedUserId = Guid.NewGuid();
        var expectedResult = new RegisterResult(expectedUserId, email);

        _authenticationService
            .RegisterAsync(email, password, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUserId, result.UserId);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task Handle_ValidCommand_DelegatesToAuthenticationService()
    {
        // Arrange
        var email = "delegate@example.com";
        var password = "Password123";
        var command = new RegisterCommand(email, password);

        _authenticationService
            .RegisterAsync(email, password, Arg.Any<CancellationToken>())
            .Returns(new RegisterResult(Guid.NewGuid(), email));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _authenticationService
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

        _authenticationService
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

        _authenticationService
            .RegisterAsync(email, password, token)
            .Returns(new RegisterResult(Guid.NewGuid(), email));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _authenticationService
            .Received(1)
            .RegisterAsync(email, password, token);
    }
}
