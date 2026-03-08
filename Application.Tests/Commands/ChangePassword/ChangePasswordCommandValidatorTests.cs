namespace BackBase.Application.Tests.Commands.ChangePassword;

using BackBase.Application.Commands.ChangePassword;
using FluentValidation.TestHelper;

public sealed class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass1!", "NewStrong1!");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WeakNewPasswordAndSameAsCurrent_HasValidationErrors()
    {
        // Arrange — weak password that is same as current
        var command = new ChangePasswordCommand(Guid.NewGuid(), "SamePass1!", "SamePass1!");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must be different from current password");
    }

    [Fact]
    public void Validate_WeakNewPassword_HasValidationErrors()
    {
        // Arrange — password without uppercase
        var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass1!", "weak1234");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
