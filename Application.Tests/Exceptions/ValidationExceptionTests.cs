namespace BackBase.Application.Tests.Exceptions;

using BackBase.Application.Exceptions;
using FluentValidation.Results;

public sealed class ValidationExceptionTests
{
    private const string EmailProperty = "Email";
    private const string PasswordProperty = "Password";
    private const string EmailError = "Email is required";
    private const string PasswordErrorMin = "Password must be at least 8 characters";
    private const string PasswordErrorDigit = "Password must contain at least one digit";

    [Fact]
    public void Constructor_WithFailures_GroupsErrorsByPropertyName()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new(EmailProperty, EmailError),
            new(PasswordProperty, PasswordErrorMin),
            new(PasswordProperty, PasswordErrorDigit)
        };

        // Act
        var exception = new ValidationException(failures);

        // Assert
        Assert.Equal("One or more validation errors occurred.", exception.Message);
        Assert.Equal(2, exception.Errors.Count);
        Assert.Single(exception.Errors[EmailProperty]);
        Assert.Equal(EmailError, exception.Errors[EmailProperty][0]);
        Assert.Equal(2, exception.Errors[PasswordProperty].Length);
        Assert.Contains(PasswordErrorMin, exception.Errors[PasswordProperty]);
        Assert.Contains(PasswordErrorDigit, exception.Errors[PasswordProperty]);
    }

    [Fact]
    public void Constructor_Parameterless_CreatesEmptyErrors()
    {
        // Arrange & Act
        var exception = new ValidationException();

        // Assert
        Assert.Equal("One or more validation errors occurred.", exception.Message);
        Assert.Empty(exception.Errors);
    }
}
