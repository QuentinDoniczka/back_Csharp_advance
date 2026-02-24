namespace BackBase.Application.Tests.Commands.RefreshToken;

using BackBase.Application.Commands.RefreshToken;
using FluentValidation.TestHelper;

public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator;

    private const string ValidAccessToken = "valid-access-token";
    private const string ValidRefreshToken = "valid-refresh-token";

    public RefreshTokenCommandValidatorTests()
    {
        _validator = new RefreshTokenCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        // Arrange
        var command = new RefreshTokenCommand(ValidAccessToken, ValidRefreshToken);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyAccessToken_HasValidationError()
    {
        // Arrange
        var command = new RefreshTokenCommand("", ValidRefreshToken);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken)
            .WithErrorMessage("Access token is required");
    }

    [Fact]
    public void Validate_EmptyRefreshToken_HasValidationError()
    {
        // Arrange
        var command = new RefreshTokenCommand(ValidAccessToken, "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Refresh token is required");
    }

    [Fact]
    public void Validate_BothTokensEmpty_HasValidationErrors()
    {
        // Arrange
        var command = new RefreshTokenCommand("", "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
