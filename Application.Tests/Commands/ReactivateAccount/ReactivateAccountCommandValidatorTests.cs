namespace BackBase.Application.Tests.Commands.ReactivateAccount;

using BackBase.Application.Commands.ReactivateAccount;
using FluentValidation.TestHelper;

public sealed class ReactivateAccountCommandValidatorTests
{
    private readonly ReactivateAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidTargetUserId_PassesValidation()
    {
        // Arrange
        var command = new ReactivateAccountCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTargetUserId_HasValidationError()
    {
        // Arrange
        var command = new ReactivateAccountCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetUserId)
            .WithErrorMessage("Target User ID is required");
    }
}
