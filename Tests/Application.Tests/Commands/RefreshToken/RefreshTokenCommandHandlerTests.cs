namespace BackBase.Application.Tests.Commands.RefreshToken;

using BackBase.Application.Commands.RefreshToken;
using BackBase.Application.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IAuthenticationService _authenticationService;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _authenticationService = Substitute.For<IAuthenticationService>();
        _handler = new RefreshTokenCommandHandler(_authenticationService);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRefreshTokenResult()
    {
        // Arrange
        var accessToken = "expired-access-token";
        var refreshToken = "valid-refresh-token";
        var command = new RefreshTokenCommand(accessToken, refreshToken);
        var newAccessToken = "new-access-token";
        var newRefreshToken = "new-refresh-token";
        var newExpiry = DateTime.UtcNow.AddHours(1);
        var expectedResult = new RefreshTokenResult(newAccessToken, newRefreshToken, newExpiry);

        _authenticationService
            .RefreshTokenAsync(accessToken, refreshToken, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newAccessToken, result.AccessToken);
        Assert.Equal(newRefreshToken, result.RefreshToken);
        Assert.Equal(newExpiry, result.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_DelegatesToAuthenticationService()
    {
        // Arrange
        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var command = new RefreshTokenCommand(accessToken, refreshToken);

        _authenticationService
            .RefreshTokenAsync(accessToken, refreshToken, Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("new-token", "new-refresh", DateTime.UtcNow));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _authenticationService
            .Received(1)
            .RefreshTokenAsync(accessToken, refreshToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var accessToken = "invalid-access-token";
        var refreshToken = "invalid-refresh-token";
        var command = new RefreshTokenCommand(accessToken, refreshToken);

        _authenticationService
            .RefreshTokenAsync(accessToken, refreshToken, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToCancellationToken()
    {
        // Arrange
        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var command = new RefreshTokenCommand(accessToken, refreshToken);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _authenticationService
            .RefreshTokenAsync(accessToken, refreshToken, token)
            .Returns(new RefreshTokenResult("new-token", "new-refresh", DateTime.UtcNow));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _authenticationService
            .Received(1)
            .RefreshTokenAsync(accessToken, refreshToken, token);
    }
}
