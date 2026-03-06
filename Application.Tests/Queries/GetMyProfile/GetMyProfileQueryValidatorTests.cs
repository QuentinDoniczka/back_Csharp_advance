namespace BackBase.Application.Tests.Queries.GetMyProfile;

using BackBase.Application.Queries.GetMyProfile;
using FluentValidation.TestHelper;

public sealed class GetMyProfileQueryValidatorTests
{
    private readonly GetMyProfileQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidUserId_PassesValidation()
    {
        // Arrange
        var query = new GetMyProfileQuery(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUserId_HasValidationError()
    {
        // Arrange
        var query = new GetMyProfileQuery(Guid.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }
}
