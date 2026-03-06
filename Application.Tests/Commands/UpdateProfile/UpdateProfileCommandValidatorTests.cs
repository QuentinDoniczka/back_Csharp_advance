namespace BackBase.Application.Tests.Commands.UpdateProfile;

using BackBase.Application.Commands.UpdateProfile;
using FluentValidation.TestHelper;

public sealed class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand(Guid.NewGuid(), "PlayerOne", "https://example.com/avatar.png");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_InvalidInputs_HasValidationErrors()
    {
        // Arrange — short display name, invalid avatar URL
        var command = new UpdateProfileCommand(Guid.Empty, "A", "not-a-url");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
        result.ShouldHaveValidationErrorFor(x => x.AvatarUrl);
    }

    [Fact]
    public void Validate_NullAvatarUrl_PassesValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand(Guid.NewGuid(), "PlayerOne", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
