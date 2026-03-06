namespace BackBase.Application.Tests.Commands.DeactivateAccount;

using BackBase.Application.Commands.DeactivateAccount;
using FluentValidation.TestHelper;

public sealed class DeactivateAccountCommandValidatorTests
{
    private readonly DeactivateAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidUserId_PassesValidation()
    {
        // Arrange
        var command = new DeactivateAccountCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUserId_HasValidationError()
    {
        // Arrange
        var command = new DeactivateAccountCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }
}
