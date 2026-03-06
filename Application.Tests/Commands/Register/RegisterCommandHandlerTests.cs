namespace BackBase.Application.Tests.Commands.Register;

using BackBase.Application.Commands.Register;
using BackBase.Domain.Constants;
using BackBase.Domain.Interfaces;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public sealed class RegisterCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly RegisterCommandHandler _handler;

    private const string ValidEmail = "player@example.com";
    private const string ValidPassword = "StrongPass1";

    public RegisterCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _handler = new RegisterCommandHandler(_identityService, _userProfileRepository);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRegisterResultWithCorrectUserId()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, ValidPassword);
        var expectedUserId = Guid.NewGuid();

        _identityService
            .RegisterAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(expectedUserId, ValidEmail));

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
        var command = new RegisterCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();

        _identityService
            .RegisterAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(userId, ValidEmail));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ValidEmail, result.Email);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRegisterAsyncWithCorrectArguments()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, ValidPassword);

        _identityService
            .RegisterAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(Guid.NewGuid(), ValidEmail));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _identityService
            .Received(1)
            .RegisterAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, ValidPassword);

        _identityService
            .RegisterAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("User already exists"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("User already exists", exception.Message);
    }

    [Fact]
    public async Task Handle_ValidCommand_AssignsMemberRoleToNewUser()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();

        _identityService
            .RegisterAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(userId, ValidEmail));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _identityService
            .Received(1)
            .AssignRoleAsync(userId, AppRoles.Member, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationTokenPassed_ForwardsToCancellationToken()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, ValidPassword);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _identityService
            .RegisterAsync(ValidEmail, ValidPassword, token)
            .Returns(new IdentityUserResult(Guid.NewGuid(), ValidEmail));

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _identityService
            .Received(1)
            .RegisterAsync(ValidEmail, ValidPassword, token);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserProfile()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, ValidPassword);
        var userId = Guid.NewGuid();

        _identityService
            .RegisterAsync(ValidEmail, ValidPassword, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserResult(userId, ValidEmail));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _userProfileRepository
            .Received(1)
            .AddAsync(Arg.Is<BackBase.Domain.Entities.UserProfile>(p => p.UserId == userId && p.DisplayName == "player"), Arg.Any<CancellationToken>());
    }
}
