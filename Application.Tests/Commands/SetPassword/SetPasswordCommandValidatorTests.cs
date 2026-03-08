namespace BackBase.Application.Tests.Commands.SetPassword;

using BackBase.Application.Commands.SetPassword;
using FluentValidation.TestHelper;

public sealed class SetPasswordCommandValidatorTests
{
    private readonly SetPasswordCommandValidator _validator;

    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const string ValidPassword = "StrongPass1";

    public SetPasswordCommandValidatorTests()
    {
        _validator = new SetPasswordCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        // Arrange
        var command = new SetPasswordCommand(ValidUserId, ValidPassword);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUserId_HasValidationError()
    {
        // Arrange
        var command = new SetPasswordCommand(Guid.Empty, ValidPassword);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public void Validate_EmptyPassword_HasValidationError()
    {
        // Arrange
        var command = new SetPasswordCommand(ValidUserId, "");

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
        var command = new SetPasswordCommand(ValidUserId, "Abc1xyz");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters");
    }

    [Fact]
    public void Validate_PasswordNoUppercase_HasValidationError()
    {
        // Arrange
        var command = new SetPasswordCommand(ValidUserId, "lowercase1");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter");
    }

    [Fact]
    public void Validate_PasswordNoLowercase_HasValidationError()
    {
        // Arrange
        var command = new SetPasswordCommand(ValidUserId, "UPPERCASE1");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one lowercase letter");
    }

    [Fact]
    public void Validate_PasswordNoDigit_HasValidationError()
    {
        // Arrange
        var command = new SetPasswordCommand(ValidUserId, "NoDigitHere");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one digit");
    }
}
