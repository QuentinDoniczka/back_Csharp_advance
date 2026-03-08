namespace BackBase.Application.Tests.Commands.Register;

using BackBase.Application.Commands.Register;
using FluentValidation.TestHelper;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator;

    private const string ValidEmail = "player@example.com";
    private const string ValidPassword = "StrongPass1";

    public RegisterCommandValidatorTests()
    {
        _validator = new RegisterCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, ValidPassword);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_HasValidationError()
    {
        // Arrange
        var command = new RegisterCommand("", ValidPassword);

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
        var command = new RegisterCommand("not-an-email", ValidPassword);

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
        var command = new RegisterCommand(ValidEmail, "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required");
    }

    [Fact]
    public void Validate_PasswordTooShort_HasValidationError()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, "Abc1xyz");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters");
    }

    [Fact]
    public void Validate_PasswordWithoutUppercase_HasValidationError()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, "lowercase1");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter");
    }

    [Fact]
    public void Validate_PasswordWithoutLowercase_HasValidationError()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, "UPPERCASE1");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one lowercase letter");
    }

    [Fact]
    public void Validate_PasswordWithoutDigit_HasValidationError()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, "NoDigitHere");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one digit");
    }

    [Fact]
    public void Validate_PasswordExactlyEightCharacters_HasNoPasswordLengthError()
    {
        // Arrange
        var command = new RegisterCommand(ValidEmail, "Abcdefg1");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
