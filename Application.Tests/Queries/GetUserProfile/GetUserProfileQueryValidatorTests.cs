namespace BackBase.Application.Tests.Queries.GetUserProfile;

using BackBase.Application.Queries.GetUserProfile;
using FluentValidation.TestHelper;

public sealed class GetUserProfileQueryValidatorTests
{
    private readonly GetUserProfileQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidUserId_PassesValidation()
    {
        // Arrange
        var query = new GetUserProfileQuery(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUserId_HasValidationError()
    {
        // Arrange
        var query = new GetUserProfileQuery(Guid.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }
}
