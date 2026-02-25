namespace BackBase.Application.Tests.Commands.Logout;

using BackBase.Application.Commands.Logout;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using NSubstitute;

public sealed class LogoutCommandHandlerTests
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRevokedTokenRepository _revokedTokenRepository;
    private readonly LogoutCommandHandler _handler;

    private const string ValidRefreshToken = "valid-refresh-token";
    private const string ValidJti = "valid-jti-id";

    public LogoutCommandHandlerTests()
    {
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _revokedTokenRepository = Substitute.For<IRevokedTokenRepository>();
        _handler = new LogoutCommandHandler(_jwtTokenService, _revokedTokenRepository);
    }

    private RefreshTokenInfo SetupValidTokenInfo(Guid? userId = null, DateTime? expiresAt = null)
    {
        var id = userId ?? Guid.NewGuid();
        var expires = expiresAt ?? DateTime.UtcNow.AddDays(30);
        var tokenInfo = new RefreshTokenInfo(id, ValidJti, expires);

        _jwtTokenService
            .ValidateAndExtractRefreshTokenInfo(ValidRefreshToken)
            .Returns(tokenInfo);

        _revokedTokenRepository
            .IsRevokedAsync(ValidJti, Arg.Any<CancellationToken>())
            .Returns(false);

        return tokenInfo;
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_CallsRevokeAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenInfo = SetupValidTokenInfo(userId);
        var command = new LogoutCommand(ValidRefreshToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _revokedTokenRepository
            .Received(1)
            .RevokeAsync(
                Arg.Is<RevokedToken>(t => t.Jti == ValidJti && t.UserId == userId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_CreatesRevokedTokenWithCorrectExpiresAt()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);
        SetupValidTokenInfo(expiresAt: expiresAt);
        var command = new LogoutCommand(ValidRefreshToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _revokedTokenRepository
            .Received(1)
            .RevokeAsync(
                Arg.Is<RevokedToken>(t => t.ExpiresAt == expiresAt),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ThrowsAuthenticationException()
    {
        // Arrange
        _jwtTokenService
            .ValidateAndExtractRefreshTokenInfo("invalid-token")
            .Returns((RefreshTokenInfo?)null);
        var command = new LogoutCommand("invalid-token");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ReturnsWithoutCallingRevokeAsync()
    {
        // Arrange
        var tokenInfo = new RefreshTokenInfo(Guid.NewGuid(), ValidJti, DateTime.UtcNow.AddDays(30));
        _jwtTokenService
            .ValidateAndExtractRefreshTokenInfo(ValidRefreshToken)
            .Returns(tokenInfo);

        _revokedTokenRepository
            .IsRevokedAsync(ValidJti, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new LogoutCommand(ValidRefreshToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _revokedTokenRepository
            .DidNotReceive()
            .RevokeAsync(Arg.Any<RevokedToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToIsRevokedAsync()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        SetupValidTokenInfo();
        var command = new LogoutCommand(ValidRefreshToken);

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _revokedTokenRepository
            .Received(1)
            .IsRevokedAsync(ValidJti, token);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToRevokeAsync()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        SetupValidTokenInfo();
        var command = new LogoutCommand(ValidRefreshToken);

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _revokedTokenRepository
            .Received(1)
            .RevokeAsync(Arg.Any<RevokedToken>(), token);
    }
}
