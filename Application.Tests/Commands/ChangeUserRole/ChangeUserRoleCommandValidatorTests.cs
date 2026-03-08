namespace BackBase.Application.Tests.Commands.ChangeUserRole;

using BackBase.Application.Commands.ChangeUserRole;
using FluentValidation.TestHelper;

public sealed class ChangeUserRoleCommandValidatorTests
{
    private readonly ChangeUserRoleCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new ChangeUserRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "Admin");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_AllFieldsEmpty_HasValidationErrors()
    {
        // Arrange
        var command = new ChangeUserRoleCommand(Guid.Empty, Guid.Empty, "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CallerUserId);
        result.ShouldHaveValidationErrorFor(x => x.TargetUserId);
        result.ShouldHaveValidationErrorFor(x => x.NewRole);
    }
}
