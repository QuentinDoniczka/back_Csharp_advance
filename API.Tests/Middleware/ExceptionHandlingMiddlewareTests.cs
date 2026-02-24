namespace BackBase.API.Tests.Middleware;

using System.Text.Json;
using BackBase.API.Middleware;
using BackBase.Application.Exceptions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
    }

    private static DefaultHttpContext CreateHttpContextWithBodyStream()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextDelegateAndKeeps200()
    {
        // Arrange
        var isNextCalled = false;
        RequestDelegate next = _ =>
        {
            isNextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(isNextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400StatusCode()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required"),
            new("Password", "Password is required")
        };
        var validationException = new ValidationException(failures);

        RequestDelegate next = _ => throw validationException;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_ResponseContainsValidationError()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required")
        };
        var validationException = new ValidationException(failures);

        RequestDelegate next = _ => throw validationException;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("Validation Error", body);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_ResponseContainsErrorsDictionary()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required"),
            new("Password", "Password is required")
        };
        var validationException = new ValidationException(failures);

        RequestDelegate next = _ => throw validationException;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("errors", out var errorsElement));
        Assert.True(errorsElement.TryGetProperty("Email", out _));
        Assert.True(errorsElement.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task InvokeAsync_AuthenticationException_Returns401StatusCode()
    {
        // Arrange
        var authException = new AuthenticationException("Invalid credentials");

        RequestDelegate next = _ => throw authException;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticationException_ResponseContainsAuthenticationFailed()
    {
        // Arrange
        var authException = new AuthenticationException("Invalid credentials");

        RequestDelegate next = _ => throw authException;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("Authentication Failed", body);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticationException_ResponseContainsExceptionMessage()
    {
        // Arrange
        var authException = new AuthenticationException("Invalid credentials");

        RequestDelegate next = _ => throw authException;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("Invalid credentials", body);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500StatusCode()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("Something went wrong");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_ResponseContainsGenericErrorMessage()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("Something went wrong");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("An unexpected error occurred", body);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_LogsError()
    {
        // Arrange
        var unhandledException = new InvalidOperationException("Something went wrong");
        RequestDelegate next = _ => throw unhandledException;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContextWithBodyStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            unhandledException,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
