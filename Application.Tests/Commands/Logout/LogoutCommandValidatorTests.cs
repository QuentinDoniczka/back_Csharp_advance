namespace BackBase.Application.Tests.Commands.Logout;

using BackBase.Application.Commands.Logout;
using FluentValidation.TestHelper;

public sealed class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator;

    private const string ValidRefreshToken = "valid-refresh-token";

    public LogoutCommandValidatorTests()
    {
        _validator = new LogoutCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        // Arrange
        var command = new LogoutCommand(ValidRefreshToken);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyRefreshToken_HasValidationError()
    {
        // Arrange
        var command = new LogoutCommand("");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Refresh token is required");
    }

    [Fact]
    public void Validate_NullRefreshToken_HasValidationError()
    {
        // Arrange
        var command = new LogoutCommand(null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
