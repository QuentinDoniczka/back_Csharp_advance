namespace BackBase.Application.Tests.Commands.RefreshToken;

using BackBase.Application.Commands.RefreshToken;
using BackBase.Domain.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Interfaces;
using NSubstitute;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRevokedTokenRepository _revokedTokenRepository;
    private readonly IIdentityService _identityService;
    private readonly RefreshTokenCommandHandler _handler;

    private const string ValidRefreshToken = "valid-refresh-token";
    private const string NewAccessToken = "new-access-token";
    private const string NewRefreshToken = "new-refresh-token";
    private const string UserEmail = "user@example.com";
    private const string ValidJti = "valid-jti-id";

    public RefreshTokenCommandHandlerTests()
    {
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _revokedTokenRepository = Substitute.For<IRevokedTokenRepository>();
        _identityService = Substitute.For<IIdentityService>();
        _handler = new RefreshTokenCommandHandler(_jwtTokenService, _revokedTokenRepository, _identityService);
    }

    private void SetupValidRefreshFlow(Guid userId, DateTime accessExpiry)
    {
        IReadOnlyList<string> roles = new List<string> { AppRoles.Member }.AsReadOnly();
        var tokenInfo = new RefreshTokenInfo(userId, ValidJti, DateTime.UtcNow.AddDays(30));
        _jwtTokenService.ValidateAndExtractRefreshTokenInfo(ValidRefreshToken).Returns(tokenInfo);

        _revokedTokenRepository
            .IsRevokedAsync(ValidJti, Arg.Any<CancellationToken>())
            .Returns(false);

        _identityService
            .IsBannedAsync(userId, Arg.Any<CancellationToken>())
            .Returns(false);

        _identityService
            .FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(userId, UserEmail));

        _identityService
            .GetRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(roles);

        _jwtTokenService
            .GenerateAccessToken(userId, UserEmail, Arg.Any<IReadOnlyList<string>>())
            .Returns((NewAccessToken, accessExpiry));

        _jwtTokenService
            .GenerateRefreshToken(userId, UserEmail)
            .Returns((NewRefreshToken, DateTime.UtcNow.AddDays(30)));
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCorrectAccessToken()
    {
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidRefreshFlow(userId, accessExpiry);
        var command = new RefreshTokenCommand(ValidRefreshToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(NewAccessToken, result.AccessToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCorrectRefreshToken()
    {
        var userId = Guid.NewGuid();
        SetupValidRefreshFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new RefreshTokenCommand(ValidRefreshToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(NewRefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCorrectExpiryDate()
    {
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidRefreshFlow(userId, accessExpiry);
        var command = new RefreshTokenCommand(ValidRefreshToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(accessExpiry, result.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsValidateAndExtractRefreshTokenInfo()
    {
        var userId = Guid.NewGuid();
        SetupValidRefreshFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new RefreshTokenCommand(ValidRefreshToken);

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenService
            .Received(1)
            .ValidateAndExtractRefreshTokenInfo(ValidRefreshToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsFindByIdAsyncWithCorrectUserId()
    {
        var userId = Guid.NewGuid();
        SetupValidRefreshFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new RefreshTokenCommand(ValidRefreshToken);

        await _handler.Handle(command, CancellationToken.None);

        await _identityService
            .Received(1)
            .FindByIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_GeneratesNewAccessTokenWithCorrectUserIdEmailAndRoles()
    {
        var userId = Guid.NewGuid();
        SetupValidRefreshFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new RefreshTokenCommand(ValidRefreshToken);

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenService
            .Received(1)
            .GenerateAccessToken(userId, UserEmail, Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task Handle_ValidCommand_GeneratesNewRefreshToken()
    {
        var userId = Guid.NewGuid();
        SetupValidRefreshFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new RefreshTokenCommand(ValidRefreshToken);

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenService
            .Received(1)
            .GenerateRefreshToken(userId, UserEmail);
    }

    [Fact]
    public async Task Handle_NullTokenInfo_ThrowsAuthenticationException()
    {
        _jwtTokenService
            .ValidateAndExtractRefreshTokenInfo("invalid-token")
            .Returns((RefreshTokenInfo?)null);
        var command = new RefreshTokenCommand("invalid-token");

        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task Handle_RevokedToken_ThrowsAuthenticationException()
    {
        var userId = Guid.NewGuid();
        var tokenInfo = new RefreshTokenInfo(userId, ValidJti, DateTime.UtcNow.AddDays(30));
        _jwtTokenService.ValidateAndExtractRefreshTokenInfo(ValidRefreshToken).Returns(tokenInfo);

        _revokedTokenRepository
            .IsRevokedAsync(ValidJti, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new RefreshTokenCommand(ValidRefreshToken);

        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Refresh token has been revoked", exception.Message);
    }

    [Fact]
    public async Task Handle_BannedUser_ThrowsAuthenticationException()
    {
        var userId = Guid.NewGuid();
        var tokenInfo = new RefreshTokenInfo(userId, ValidJti, DateTime.UtcNow.AddDays(30));
        _jwtTokenService.ValidateAndExtractRefreshTokenInfo(ValidRefreshToken).Returns(tokenInfo);

        _revokedTokenRepository
            .IsRevokedAsync(ValidJti, Arg.Any<CancellationToken>())
            .Returns(false);

        _identityService
            .IsBannedAsync(userId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new RefreshTokenCommand(ValidRefreshToken);

        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("User account is suspended", exception.Message);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsAuthenticationException()
    {
        var userId = Guid.NewGuid();
        var tokenInfo = new RefreshTokenInfo(userId, ValidJti, DateTime.UtcNow.AddDays(30));
        _jwtTokenService.ValidateAndExtractRefreshTokenInfo(ValidRefreshToken).Returns(tokenInfo);

        _revokedTokenRepository
            .IsRevokedAsync(ValidJti, Arg.Any<CancellationToken>())
            .Returns(false);

        _identityService
            .IsBannedAsync(userId, Arg.Any<CancellationToken>())
            .Returns(false);

        _identityService
            .FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((IdentityUserResult?)null);

        var command = new RefreshTokenCommand(ValidRefreshToken);

        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToIsRevokedAsync()
    {
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        SetupValidRefreshFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new RefreshTokenCommand(ValidRefreshToken);

        await _handler.Handle(command, token);

        await _revokedTokenRepository
            .Received(1)
            .IsRevokedAsync(ValidJti, token);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToFindByIdAsync()
    {
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        SetupValidRefreshFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new RefreshTokenCommand(ValidRefreshToken);

        await _handler.Handle(command, token);

        await _identityService
            .Received(1)
            .FindByIdAsync(userId, token);
    }
}
