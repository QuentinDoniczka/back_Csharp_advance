namespace BackBase.Application.Tests.Commands.Login;

using BackBase.Application.Commands.Login;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class LoginCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly LoginCommandHandler _handler;

    private const string ValidEmail = "player@example.com";
    private const string ValidPassword = "StrongPass1";
    private const string GeneratedAccessToken = "access-token-value";
    private const string GeneratedRawRefreshToken = "refresh-token-value";
    private const string GeneratedRefreshHash = "refresh-hash-value";

    public LoginCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _handler = new LoginCommandHandler(_identityService, _jwtTokenService, _refreshTokenRepository);
    }

    private void SetupValidLoginFlow(Guid userId, string email, DateTime accessExpiry, DateTime refreshExpiry)
    {
        _identityService
            .ValidateCredentialsAsync(email, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(userId, email));

        _jwtTokenService
            .GenerateAccessToken(userId, email)
            .Returns((GeneratedAccessToken, accessExpiry));

        _jwtTokenService
            .GenerateRefreshToken()
            .Returns((GeneratedRawRefreshToken, GeneratedRefreshHash, refreshExpiry));
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthTokenResultWithCorrectAccessToken()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidLoginFlow(userId, ValidEmail, accessExpiry, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(GeneratedAccessToken, result.AccessToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthTokenResultWithCorrectRefreshToken()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(GeneratedRawRefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthTokenResultWithCorrectExpiry()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidLoginFlow(userId, ValidEmail, accessExpiry, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(accessExpiry, result.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsValidateCredentialsWithCorrectArguments()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _identityService
            .Received(1)
            .ValidateCredentialsAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsGenerateAccessTokenWithUserIdAndEmail()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtTokenService
            .Received(1)
            .GenerateAccessToken(userId, ValidEmail);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsGenerateRefreshToken()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtTokenService
            .Received(1)
            .GenerateRefreshToken();
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsRefreshTokenViaAddAsync()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _refreshTokenRepository
            .Received(1)
            .AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChangesAsync()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(30));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _refreshTokenRepository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ThrowsAuthenticationException()
    {
        // Arrange
        var command = new LoginCommand("wrong@example.com", "WrongPassword1");

        _identityService
            .ValidateCredentialsAsync("wrong@example.com", "WrongPassword1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException("Invalid credentials"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid credentials", exception.Message);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_DoesNotGenerateTokens()
    {
        // Arrange
        var command = new LoginCommand("wrong@example.com", "WrongPassword1");

        _identityService
            .ValidateCredentialsAsync("wrong@example.com", "WrongPassword1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException("Invalid credentials"));

        // Act
        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        // Assert
        _jwtTokenService
            .DidNotReceive()
            .GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>());
        _jwtTokenService
            .DidNotReceive()
            .GenerateRefreshToken();
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToValidateCredentials()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var userId = Guid.NewGuid();

        _identityService
            .ValidateCredentialsAsync(ValidEmail, ValidPassword, token)
            .Returns(new IdentityUserResult(userId, ValidEmail));

        _jwtTokenService
            .GenerateAccessToken(userId, ValidEmail)
            .Returns(("token", DateTime.UtcNow));

        _jwtTokenService
            .GenerateRefreshToken()
            .Returns(("refresh", "hash", DateTime.UtcNow.AddDays(30)));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _identityService
            .Received(1)
            .ValidateCredentialsAsync(ValidEmail, ValidPassword, token);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToRepositoryAddAsync()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var userId = Guid.NewGuid();

        _identityService
            .ValidateCredentialsAsync(ValidEmail, ValidPassword, token)
            .Returns(new IdentityUserResult(userId, ValidEmail));

        _jwtTokenService
            .GenerateAccessToken(userId, ValidEmail)
            .Returns(("token", DateTime.UtcNow));

        _jwtTokenService
            .GenerateRefreshToken()
            .Returns(("refresh", "hash", DateTime.UtcNow.AddDays(30)));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _refreshTokenRepository
            .Received(1)
            .AddAsync(Arg.Any<RefreshToken>(), token);
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToRepositorySaveChanges()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var userId = Guid.NewGuid();

        _identityService
            .ValidateCredentialsAsync(ValidEmail, ValidPassword, token)
            .Returns(new IdentityUserResult(userId, ValidEmail));

        _jwtTokenService
            .GenerateAccessToken(userId, ValidEmail)
            .Returns(("token", DateTime.UtcNow));

        _jwtTokenService
            .GenerateRefreshToken()
            .Returns(("refresh", "hash", DateTime.UtcNow.AddDays(30)));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _refreshTokenRepository
            .Received(1)
            .SaveChangesAsync(token);
    }
}
