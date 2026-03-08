namespace BackBase.Application.Tests.Behaviors;

using BackBase.Application.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using ValidationException = BackBase.Application.Exceptions.ValidationException;

public record TestRequest(string Email) : IRequest<string>;

public sealed class ValidationBehaviorTests
{
    private const string ExpectedResponse = "handler-response";
    private const string PropertyName = "Email";
    private const string ErrorMessage = "Email is required";

    [Fact]
    public async Task Handle_NoValidators_PassesThroughToNextHandler()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("test@example.com");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns(ExpectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(ExpectedResponse, result);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_ValidatorsPassValidation_PassesThroughToNextHandler()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("test@example.com");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns(ExpectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(ExpectedResponse, result);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_ValidatorsFail_ThrowsValidationExceptionWithErrors()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new(PropertyName, ErrorMessage)
        };
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));
        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("");
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.Contains(PropertyName, exception.Errors.Keys);
        Assert.Contains(ErrorMessage, exception.Errors[PropertyName]);
        await next.DidNotReceive().Invoke();
    }
}
