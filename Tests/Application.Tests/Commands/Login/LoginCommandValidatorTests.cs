namespace BackBase.Application.Tests.Commands.Login;

using BackBase.Application.Commands.Login;
using FluentValidation.TestHelper;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        // Arrange
        var command = new LoginCommand("player@example.com", "StrongPass1");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_HasValidationError()
    {
        // Arrange
        var command = new LoginCommand("", "StrongPass1");

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
        var command = new LoginCommand("not-an-email", "StrongPass1");

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
        var command = new LoginCommand("player@example.com", "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required");
    }

    [Fact]
    public void Validate_ValidEmailAndPassword_HasNoErrors()
    {
        // Arrange
        var command = new LoginCommand("user@domain.org", "anypassword");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
