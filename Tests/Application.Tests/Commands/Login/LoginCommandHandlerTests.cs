namespace BackBase.Application.Tests.Commands.Login;

using BackBase.Application.Commands.Login;
using BackBase.Application.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class LoginCommandHandlerTests
{
    private readonly IAuthenticationService _authenticationService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _authenticationService = Substitute.For<IAuthenticationService>();
        _handler = new LoginCommandHandler(_authenticationService);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsLoginResult()
    {
        // Arrange
        var email = "player@example.com";
        var password = "StrongPass1";
        var command = new LoginCommand(email, password);
        var expectedAccessToken = "access-token-value";
        var expectedRefreshToken = "refresh-token-value";
        var expectedExpiry = DateTime.UtcNow.AddHours(1);
        var expectedResult = new LoginResult(expectedAccessToken, expectedRefreshToken, expectedExpiry);

        _authenticationService
            .LoginAsync(email, password, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAccessToken, result.AccessToken);
        Assert.Equal(expectedRefreshToken, result.RefreshToken);
        Assert.Equal(expectedExpiry, result.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_DelegatesToAuthenticationService()
    {
        // Arrange
        var email = "delegate@example.com";
        var password = "Password123";
        var command = new LoginCommand(email, password);

        _authenticationService
            .LoginAsync(email, password, Arg.Any<CancellationToken>())
            .Returns(new LoginResult("token", "refresh", DateTime.UtcNow));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _authenticationService
            .Received(1)
            .LoginAsync(email, password, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var email = "wrong@example.com";
        var password = "WrongPassword1";
        var command = new LoginCommand(email, password);

        _authenticationService
            .LoginAsync(email, password, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid credentials", exception.Message);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToCancellationToken()
    {
        // Arrange
        var email = "cancel@example.com";
        var password = "Password123";
        var command = new LoginCommand(email, password);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _authenticationService
            .LoginAsync(email, password, token)
            .Returns(new LoginResult("token", "refresh", DateTime.UtcNow));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _authenticationService
            .Received(1)
            .LoginAsync(email, password, token);
    }
}
