namespace BackBase.Application.Tests.Commands.RefreshToken;

using System.Security.Claims;
using BackBase.Application.Commands.RefreshToken;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Interfaces;
using NSubstitute;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityService _identityService;
    private readonly RefreshTokenCommandHandler _handler;

    private const string ExpiredAccessToken = "expired-access-token";
    private const string ValidRawRefreshToken = "valid-refresh-token";
    private const string StoredHash = "stored-hash";
    private const string NewAccessToken = "new-access-token";
    private const string NewRawRefreshToken = "new-refresh-token";
    private const string NewRefreshHash = "new-refresh-hash";
    private const string UserEmail = "user@example.com";

    public RefreshTokenCommandHandlerTests()
    {
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _identityService = Substitute.For<IIdentityService>();
        _handler = new RefreshTokenCommandHandler(_jwtTokenService, _refreshTokenRepository, _identityService);
    }

    private static ClaimsPrincipal CreatePrincipalWithSubClaim(Guid userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString())
        }));
    }

    private static ClaimsPrincipal CreatePrincipalWithNameIdentifierClaim(Guid userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));
    }

    private void SetupValidRefreshFlow(
        Guid userId,
        ClaimsPrincipal principal,
        DateTime newAccessExpiry,
        DateTime newRefreshExpiry)
    {
        _jwtTokenService.GetPrincipalFromExpiredToken(ExpiredAccessToken).Returns(principal);
        _jwtTokenService.HashToken(ValidRawRefreshToken).Returns(StoredHash);

        var storedToken = BackBase.Domain.Entities.RefreshToken.Create(StoredHash, userId, DateTime.UtcNow.AddDays(30));
        _refreshTokenRepository
            .GetByTokenHashAndUserIdAsync(StoredHash, userId, Arg.Any<CancellationToken>())
            .Returns(storedToken);

        _identityService
            .FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(userId, UserEmail));

        _jwtTokenService
            .GenerateAccessToken(userId, UserEmail)
            .Returns((NewAccessToken, newAccessExpiry));

        _jwtTokenService
            .GenerateRefreshToken()
            .Returns((NewRawRefreshToken, NewRefreshHash, newRefreshExpiry));
    }

    // --- Happy path tests ---

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCorrectAccessToken()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidRefreshFlow(userId, principal, accessExpiry, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(NewAccessToken, result.AccessToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCorrectRefreshToken()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(NewRawRefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCorrectExpiryDate()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidRefreshFlow(userId, principal, accessExpiry, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(accessExpiry, result.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_ValidCommandWithSubClaim_ReturnsAuthTokenResult()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithSubClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NewAccessToken, result.AccessToken);
    }

    // --- Verification of service calls ---

    [Fact]
    public async Task Handle_ValidCommand_CallsGetPrincipalFromExpiredToken()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtTokenService
            .Received(1)
            .GetPrincipalFromExpiredToken(ExpiredAccessToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsHashTokenWithRawRefreshToken()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtTokenService
            .Received(1)
            .HashToken(ValidRawRefreshToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsFindByIdAsyncWithCorrectUserId()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _identityService
            .Received(1)
            .FindByIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsNewRefreshTokenViaAddAsync()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _refreshTokenRepository
            .Received(1)
            .AddAsync(Arg.Any<BackBase.Domain.Entities.RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChangesAsync()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _refreshTokenRepository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_GeneratesNewAccessTokenWithCorrectUserIdAndEmail()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtTokenService
            .Received(1)
            .GenerateAccessToken(userId, UserEmail);
    }

    [Fact]
    public async Task Handle_ValidCommand_GeneratesNewRefreshToken()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtTokenService
            .Received(1)
            .GenerateRefreshToken();
    }

    // --- Error path: null principal ---

    [Fact]
    public async Task Handle_NullPrincipal_ThrowsAuthenticationException()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid-access-token", "some-refresh-token");

        _jwtTokenService
            .GetPrincipalFromExpiredToken("invalid-access-token")
            .Returns((ClaimsPrincipal?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid access token", exception.Message);
    }

    // --- Error path: missing userId claim ---

    [Fact]
    public async Task Handle_PrincipalWithoutUserIdClaim_ThrowsAuthenticationException()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var principalWithNoClaims = new ClaimsPrincipal(new ClaimsIdentity());

        _jwtTokenService
            .GetPrincipalFromExpiredToken(ExpiredAccessToken)
            .Returns(principalWithNoClaims);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid access token", exception.Message);
    }

    // --- Error path: non-parseable userId ---

    [Fact]
    public async Task Handle_PrincipalWithNonGuidUserId_ThrowsAuthenticationException()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var principalWithBadId = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "not-a-guid")
        }));

        _jwtTokenService
            .GetPrincipalFromExpiredToken(ExpiredAccessToken)
            .Returns(principalWithBadId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid access token", exception.Message);
    }

    // --- Error path: stored token is null ---

    [Fact]
    public async Task Handle_StoredTokenNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);

        _jwtTokenService.GetPrincipalFromExpiredToken(ExpiredAccessToken).Returns(principal);
        _jwtTokenService.HashToken(ValidRawRefreshToken).Returns("bad-hash");

        _refreshTokenRepository
            .GetByTokenHashAndUserIdAsync("bad-hash", userId, Arg.Any<CancellationToken>())
            .Returns((BackBase.Domain.Entities.RefreshToken?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid refresh token", exception.Message);
    }

    // --- Error path: stored token is revoked ---

    [Fact]
    public async Task Handle_StoredTokenIsRevoked_ThrowsAuthenticationException()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);

        _jwtTokenService.GetPrincipalFromExpiredToken(ExpiredAccessToken).Returns(principal);
        _jwtTokenService.HashToken(ValidRawRefreshToken).Returns(StoredHash);

        var revokedToken = BackBase.Domain.Entities.RefreshToken.Create(StoredHash, userId, DateTime.UtcNow.AddDays(30));
        revokedToken.Revoke();

        _refreshTokenRepository
            .GetByTokenHashAndUserIdAsync(StoredHash, userId, Arg.Any<CancellationToken>())
            .Returns(revokedToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid refresh token", exception.Message);
    }

    // --- Error path: stored token is expired ---

    [Fact]
    public async Task Handle_StoredTokenIsExpired_ThrowsAuthenticationException()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);

        _jwtTokenService.GetPrincipalFromExpiredToken(ExpiredAccessToken).Returns(principal);
        _jwtTokenService.HashToken(ValidRawRefreshToken).Returns(StoredHash);

        var expiredToken = BackBase.Domain.Entities.RefreshToken.Create(StoredHash, userId, DateTime.UtcNow.AddSeconds(-1));

        _refreshTokenRepository
            .GetByTokenHashAndUserIdAsync(StoredHash, userId, Arg.Any<CancellationToken>())
            .Returns(expiredToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid refresh token", exception.Message);
    }

    // --- Error path: user not found ---

    [Fact]
    public async Task Handle_UserNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);

        _jwtTokenService.GetPrincipalFromExpiredToken(ExpiredAccessToken).Returns(principal);
        _jwtTokenService.HashToken(ValidRawRefreshToken).Returns(StoredHash);

        var storedToken = BackBase.Domain.Entities.RefreshToken.Create(StoredHash, userId, DateTime.UtcNow.AddDays(30));
        _refreshTokenRepository
            .GetByTokenHashAndUserIdAsync(StoredHash, userId, Arg.Any<CancellationToken>())
            .Returns(storedToken);

        _identityService
            .FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((IdentityUserResult?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid refresh token", exception.Message);
    }

    // --- Cancellation token forwarding ---

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToRepository()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Override to use the specific token for matching
        _refreshTokenRepository
            .GetByTokenHashAndUserIdAsync(StoredHash, userId, token)
            .Returns(BackBase.Domain.Entities.RefreshToken.Create(StoredHash, userId, DateTime.UtcNow.AddDays(30)));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _refreshTokenRepository
            .Received(1)
            .GetByTokenHashAndUserIdAsync(StoredHash, userId, token);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToFindByIdAsync()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Override to use the specific token
        _refreshTokenRepository
            .GetByTokenHashAndUserIdAsync(StoredHash, userId, token)
            .Returns(BackBase.Domain.Entities.RefreshToken.Create(StoredHash, userId, DateTime.UtcNow.AddDays(30)));

        _identityService
            .FindByIdAsync(userId, token)
            .Returns(new IdentityUserResult(userId, UserEmail));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _identityService
            .Received(1)
            .FindByIdAsync(userId, token);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToSaveChangesAsync()
    {
        // Arrange
        var command = new RefreshTokenCommand(ExpiredAccessToken, ValidRawRefreshToken);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var userId = Guid.NewGuid();
        var principal = CreatePrincipalWithNameIdentifierClaim(userId);
        SetupValidRefreshFlow(userId, principal, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Override to use the specific token
        _refreshTokenRepository
            .GetByTokenHashAndUserIdAsync(StoredHash, userId, token)
            .Returns(BackBase.Domain.Entities.RefreshToken.Create(StoredHash, userId, DateTime.UtcNow.AddDays(30)));

        _identityService
            .FindByIdAsync(userId, token)
            .Returns(new IdentityUserResult(userId, UserEmail));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _refreshTokenRepository
            .Received(1)
            .SaveChangesAsync(token);
    }
}
