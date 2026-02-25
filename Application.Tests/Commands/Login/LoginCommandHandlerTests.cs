namespace BackBase.Application.Tests.Commands.Login;

using BackBase.Application.Commands.Login;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class LoginCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly LoginCommandHandler _handler;

    private const string ValidEmail = "player@example.com";
    private const string ValidPassword = "StrongPass1";
    private const string GeneratedAccessToken = "access-token-value";
    private const string GeneratedRefreshToken = "refresh-token-value";

    public LoginCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _handler = new LoginCommandHandler(_identityService, _jwtTokenService);
    }

    private void SetupValidLoginFlow(Guid userId, string email, DateTime accessExpiry)
    {
        _identityService
            .ValidateCredentialsAsync(email, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(userId, email));

        _jwtTokenService
            .GenerateAccessToken(userId, email)
            .Returns((GeneratedAccessToken, accessExpiry));

        _jwtTokenService
            .GenerateRefreshToken(userId, email)
            .Returns((GeneratedRefreshToken, DateTime.UtcNow.AddDays(30)));
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthTokenResultWithCorrectAccessToken()
    {
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidLoginFlow(userId, ValidEmail, accessExpiry);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(GeneratedAccessToken, result.AccessToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthTokenResultWithCorrectRefreshToken()
    {
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(GeneratedRefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthTokenResultWithCorrectExpiry()
    {
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidLoginFlow(userId, ValidEmail, accessExpiry);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(accessExpiry, result.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsValidateCredentialsWithCorrectArguments()
    {
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1));

        await _handler.Handle(command, CancellationToken.None);

        await _identityService
            .Received(1)
            .ValidateCredentialsAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsGenerateAccessTokenWithUserIdAndEmail()
    {
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1));

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenService
            .Received(1)
            .GenerateAccessToken(userId, ValidEmail);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsGenerateRefreshTokenWithUserIdAndEmail()
    {
        var command = new LoginCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();
        SetupValidLoginFlow(userId, ValidEmail, DateTime.UtcNow.AddHours(1));

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenService
            .Received(1)
            .GenerateRefreshToken(userId, ValidEmail);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ThrowsAuthenticationException()
    {
        var command = new LoginCommand("wrong@example.com", "WrongPassword1");

        _identityService
            .ValidateCredentialsAsync("wrong@example.com", "WrongPassword1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException("Invalid credentials"));

        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid credentials", exception.Message);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_DoesNotGenerateTokens()
    {
        var command = new LoginCommand("wrong@example.com", "WrongPassword1");

        _identityService
            .ValidateCredentialsAsync("wrong@example.com", "WrongPassword1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException("Invalid credentials"));

        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        _jwtTokenService
            .DidNotReceive()
            .GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>());
        _jwtTokenService
            .DidNotReceive()
            .GenerateRefreshToken(Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToValidateCredentials()
    {
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
            .GenerateRefreshToken(userId, ValidEmail)
            .Returns(("refresh", DateTime.UtcNow.AddDays(30)));

        await _handler.Handle(command, token);

        await _identityService
            .Received(1)
            .ValidateCredentialsAsync(ValidEmail, ValidPassword, token);
    }
}
