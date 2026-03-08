namespace BackBase.Application.Tests.Commands.Login;

using BackBase.Application.Commands.Login;
using FluentValidation.TestHelper;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    private const string ValidEmail = "player@example.com";
    private const string ValidPassword = "StrongPass1";

    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, ValidPassword);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_HasValidationError()
    {
        // Arrange
        var command = new LoginCommand("", ValidPassword);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_HasValidationError()
    {
        // Arrange
        var command = new LoginCommand("not-an-email", ValidPassword);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is not valid");
    }

    [Fact]
    public void Validate_EmptyPassword_HasValidationError()
    {
        // Arrange
        var command = new LoginCommand(ValidEmail, "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required");
    }
}
