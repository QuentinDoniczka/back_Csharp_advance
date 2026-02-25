namespace BackBase.Application.Tests.Commands.RefreshToken;

using BackBase.Application.Commands.RefreshToken;
using FluentValidation.TestHelper;

public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator;

    private const string ValidRefreshToken = "valid-refresh-token";

    public RefreshTokenCommandValidatorTests()
    {
        _validator = new RefreshTokenCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_HasNoValidationErrors()
    {
        var command = new RefreshTokenCommand(ValidRefreshToken);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyRefreshToken_HasValidationError()
    {
        var command = new RefreshTokenCommand("");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Refresh token is required");
    }
}
