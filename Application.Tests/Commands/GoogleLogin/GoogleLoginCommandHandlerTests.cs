namespace BackBase.Application.Tests.Commands.GoogleLogin;

using BackBase.Application.Commands.GoogleLogin;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Constants;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class GoogleLoginCommandHandlerTests
{
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly GoogleLoginCommandHandler _handler;

    private const string ValidIdToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.valid-google-token";
    private const string GoogleEmail = "player@gmail.com";
    private const string GoogleUserId = "google-user-id-123";
    private const string GoogleUserName = "Player One";
    private const string GeneratedAccessToken = "access-token-value";
    private const string GeneratedRefreshToken = "refresh-token-value";

    public GoogleLoginCommandHandlerTests()
    {
        _googleTokenValidator = Substitute.For<IGoogleTokenValidator>();
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _handler = new GoogleLoginCommandHandler(_googleTokenValidator, _identityService, _jwtTokenService);
    }

    private void SetupValidGoogleLoginFlow(Guid userId, DateTime accessExpiry, IReadOnlyList<string>? roles = null, bool isNewAccount = false)
    {
        roles ??= new List<string> { AppRoles.Member }.AsReadOnly();

        _googleTokenValidator
            .ValidateAsync(ValidIdToken, Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo(GoogleEmail, GoogleUserId, GoogleUserName));

        _identityService
            .FindOrCreateExternalUserAsync(GoogleEmail, ExternalProviders.Google, GoogleUserId, Arg.Any<CancellationToken>())
            .Returns(new ExternalLoginResult(userId, GoogleEmail, isNewAccount));

        _identityService
            .IsBannedAsync(userId, Arg.Any<CancellationToken>())
            .Returns(false);

        _identityService
            .GetRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(roles);

        _jwtTokenService
            .GenerateAccessToken(userId, GoogleEmail, Arg.Any<IReadOnlyList<string>>())
            .Returns((GeneratedAccessToken, accessExpiry));

        _jwtTokenService
            .GenerateRefreshToken(userId, GoogleEmail)
            .Returns((GeneratedRefreshToken, DateTime.UtcNow.AddDays(30)));
    }

    [Fact]
    public async Task Handle_ValidGoogleToken_ReturnsAuthTokenResultWithCorrectAccessToken()
    {
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidGoogleLoginFlow(userId, accessExpiry);
        var command = new GoogleLoginCommand(ValidIdToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(GeneratedAccessToken, result.Tokens.AccessToken);
    }

    [Fact]
    public async Task Handle_ValidGoogleToken_ReturnsAuthTokenResultWithCorrectRefreshToken()
    {
        var userId = Guid.NewGuid();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new GoogleLoginCommand(ValidIdToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(GeneratedRefreshToken, result.Tokens.RefreshToken);
    }

    [Fact]
    public async Task Handle_ValidGoogleToken_ReturnsAuthTokenResultWithCorrectExpiry()
    {
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidGoogleLoginFlow(userId, accessExpiry);
        var command = new GoogleLoginCommand(ValidIdToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(accessExpiry, result.Tokens.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_ValidGoogleToken_CallsGoogleTokenValidatorWithCorrectIdToken()
    {
        var userId = Guid.NewGuid();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new GoogleLoginCommand(ValidIdToken);

        await _handler.Handle(command, CancellationToken.None);

        await _googleTokenValidator
            .Received(1)
            .ValidateAsync(ValidIdToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidGoogleToken_CallsFindOrCreateExternalUserWithCorrectArgs()
    {
        var userId = Guid.NewGuid();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new GoogleLoginCommand(ValidIdToken);

        await _handler.Handle(command, CancellationToken.None);

        await _identityService
            .Received(1)
            .FindOrCreateExternalUserAsync(GoogleEmail, ExternalProviders.Google, GoogleUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidGoogleToken_CallsGetRolesAsyncWithCorrectUserId()
    {
        var userId = Guid.NewGuid();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new GoogleLoginCommand(ValidIdToken);

        await _handler.Handle(command, CancellationToken.None);

        await _identityService
            .Received(1)
            .GetRolesAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidGoogleToken_GeneratesAccessTokenWithUserIdEmailAndRoles()
    {
        var userId = Guid.NewGuid();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new GoogleLoginCommand(ValidIdToken);

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenService
            .Received(1)
            .GenerateAccessToken(userId, GoogleEmail, Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task Handle_ValidGoogleToken_GeneratesRefreshTokenWithUserIdAndEmail()
    {
        var userId = Guid.NewGuid();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1));
        var command = new GoogleLoginCommand(ValidIdToken);

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenService
            .Received(1)
            .GenerateRefreshToken(userId, GoogleEmail);
    }

    [Fact]
    public async Task Handle_NewUserWithNoRoles_AssignsMemberRole()
    {
        var userId = Guid.NewGuid();
        IReadOnlyList<string> emptyRoles = new List<string>().AsReadOnly();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1), emptyRoles);
        var command = new GoogleLoginCommand(ValidIdToken);

        await _handler.Handle(command, CancellationToken.None);

        await _identityService
            .Received(1)
            .AssignRoleAsync(userId, AppRoles.Member, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewUserWithNoRoles_UsesMemberRoleForTokenGeneration()
    {
        var userId = Guid.NewGuid();
        IReadOnlyList<string> emptyRoles = new List<string>().AsReadOnly();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1), emptyRoles);
        var command = new GoogleLoginCommand(ValidIdToken);

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenService
            .Received(1)
            .GenerateAccessToken(
                userId,
                GoogleEmail,
                Arg.Is<IReadOnlyList<string>>(r => r.Count == 1 && r[0] == AppRoles.Member));
    }

    [Fact]
    public async Task Handle_ExistingUserWithRoles_DoesNotCallAssignRoleAsync()
    {
        var userId = Guid.NewGuid();
        IReadOnlyList<string> existingRoles = new List<string> { AppRoles.Member }.AsReadOnly();
        SetupValidGoogleLoginFlow(userId, DateTime.UtcNow.AddHours(1), existingRoles);
        var command = new GoogleLoginCommand(ValidIdToken);

        await _handler.Handle(command, CancellationToken.None);

        await _identityService
            .DidNotReceive()
            .AssignRoleAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewAccount_ReturnsIsNewAccountTrue()
    {
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidGoogleLoginFlow(userId, accessExpiry, isNewAccount: true);
        var command = new GoogleLoginCommand(ValidIdToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsNewAccount);
    }

    [Fact]
    public async Task Handle_ExistingAccount_ReturnsIsNewAccountFalse()
    {
        var userId = Guid.NewGuid();
        var accessExpiry = DateTime.UtcNow.AddHours(1);
        SetupValidGoogleLoginFlow(userId, accessExpiry, isNewAccount: false);
        var command = new GoogleLoginCommand(ValidIdToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsNewAccount);
    }

    [Fact]
    public async Task Handle_InvalidGoogleToken_ThrowsAuthenticationException()
    {
        _googleTokenValidator
            .ValidateAsync("invalid-token", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException("Invalid Google ID token"));
        var command = new GoogleLoginCommand("invalid-token");

        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Invalid Google ID token", exception.Message);
    }

    [Fact]
    public async Task Handle_InvalidGoogleToken_DoesNotCallFindOrCreateExternalUser()
    {
        _googleTokenValidator
            .ValidateAsync("invalid-token", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException("Invalid Google ID token"));
        var command = new GoogleLoginCommand("invalid-token");

        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        await _identityService
            .DidNotReceive()
            .FindOrCreateExternalUserAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BannedUser_ThrowsAuthenticationException()
    {
        var userId = Guid.NewGuid();

        _googleTokenValidator
            .ValidateAsync(ValidIdToken, Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo(GoogleEmail, GoogleUserId, GoogleUserName));

        _identityService
            .FindOrCreateExternalUserAsync(GoogleEmail, ExternalProviders.Google, GoogleUserId, Arg.Any<CancellationToken>())
            .Returns(new ExternalLoginResult(userId, GoogleEmail, false));

        _identityService
            .IsBannedAsync(userId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new GoogleLoginCommand(ValidIdToken);

        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("User account is banned", exception.Message);
    }

    [Fact]
    public async Task Handle_BannedUser_DoesNotGenerateTokens()
    {
        var userId = Guid.NewGuid();

        _googleTokenValidator
            .ValidateAsync(ValidIdToken, Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo(GoogleEmail, GoogleUserId, GoogleUserName));

        _identityService
            .FindOrCreateExternalUserAsync(GoogleEmail, ExternalProviders.Google, GoogleUserId, Arg.Any<CancellationToken>())
            .Returns(new ExternalLoginResult(userId, GoogleEmail, false));

        _identityService
            .IsBannedAsync(userId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new GoogleLoginCommand(ValidIdToken);

        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        _jwtTokenService
            .DidNotReceive()
            .GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
        _jwtTokenService
            .DidNotReceive()
            .GenerateRefreshToken(Arg.Any<Guid>(), Arg.Any<string>());
    }
}
