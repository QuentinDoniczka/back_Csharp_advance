namespace BackBase.Application.Tests.Commands.GoogleLogin;

using BackBase.Application.Commands.GoogleLogin;
using FluentValidation.TestHelper;

public sealed class GoogleLoginCommandValidatorTests
{
    private readonly GoogleLoginCommandValidator _validator;

    private const string ValidIdToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.valid-google-token";

    public GoogleLoginCommandValidatorTests()
    {
        _validator = new GoogleLoginCommandValidator();
    }

    [Fact]
    public void Validate_ValidIdToken_HasNoValidationErrors()
    {
        // Arrange
        var command = new GoogleLoginCommand(ValidIdToken);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyIdToken_HasValidationError()
    {
        // Arrange
        var command = new GoogleLoginCommand("");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdToken)
            .WithErrorMessage("Google ID token is required");
    }

    [Fact]
    public void Validate_NullIdToken_HasValidationError()
    {
        // Arrange
        var command = new GoogleLoginCommand(null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdToken)
            .WithErrorMessage("Google ID token is required");
    }
}
