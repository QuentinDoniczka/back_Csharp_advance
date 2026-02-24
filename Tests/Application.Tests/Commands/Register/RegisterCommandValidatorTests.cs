namespace BackBase.Application.Tests.Commands.Register;

using BackBase.Application.Commands.Register;
using FluentValidation.TestHelper;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator;

    public RegisterCommandValidatorTests()
    {
        _validator = new RegisterCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        // Arrange
        var command = new RegisterCommand("player@example.com", "StrongPass1");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_HasValidationError()
    {
        // Arrange
        var command = new RegisterCommand("", "StrongPass1");

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
        var command = new RegisterCommand("not-an-email", "StrongPass1");

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
        var command = new RegisterCommand("player@example.com", "");

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
        var shortPassword = "Abc1xyz";
        var command = new RegisterCommand("player@example.com", shortPassword);

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
        var noUppercasePassword = "lowercase1";
        var command = new RegisterCommand("player@example.com", noUppercasePassword);

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
        var noLowercasePassword = "UPPERCASE1";
        var command = new RegisterCommand("player@example.com", noLowercasePassword);

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
        var noDigitPassword = "NoDigitHere";
        var command = new RegisterCommand("player@example.com", noDigitPassword);

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
        var exactMinLengthPassword = "Abcdefg1";
        var command = new RegisterCommand("player@example.com", exactMinLengthPassword);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
