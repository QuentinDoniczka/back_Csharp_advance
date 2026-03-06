namespace BackBase.Application.Tests.Queries.GetUserRole;

using BackBase.Application.Queries.GetUserRole;
using FluentValidation.TestHelper;

public sealed class GetUserRoleQueryValidatorTests
{
    private readonly GetUserRoleQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidTargetUserId_PassesValidation()
    {
        // Arrange
        var query = new GetUserRoleQuery(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTargetUserId_HasValidationError()
    {
        // Arrange
        var query = new GetUserRoleQuery(Guid.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetUserId)
            .WithErrorMessage("Target User ID is required");
    }
}
