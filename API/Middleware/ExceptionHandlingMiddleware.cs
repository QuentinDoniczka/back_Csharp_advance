using BackBase.Application.Constants;
using BackBase.Application.Exceptions;

namespace BackBase.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            switch (ex)
            {
                case ValidationException validationEx:
                    await HandleValidationExceptionAsync(context, validationEx);
                    break;
                case AuthenticationException authEx:
                    await HandleAuthenticationExceptionAsync(context, authEx);
                    break;
                case ForbiddenException forbiddenEx:
                    await HandleForbiddenExceptionAsync(context, forbiddenEx);
                    break;
                case NotFoundException notFoundEx:
                    await HandleNotFoundExceptionAsync(context, notFoundEx);
                    break;
                case ConflictException conflictEx:
                    await HandleConflictExceptionAsync(context, conflictEx);
                    break;
                case InvalidOperationException invalidOpEx:
                    await HandleConflictExceptionAsync(context, invalidOpEx);
                    break;
                default:
                    _logger.LogError(ex, "Unhandled exception occurred");
                    await HandleExceptionAsync(context);
                    break;
            }
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Validation Error",
            status = StatusCodes.Status400BadRequest,
            errors = exception.Errors
        });
    }

    private static async Task HandleAuthenticationExceptionAsync(HttpContext context, AuthenticationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Authentication Failed",
            status = StatusCodes.Status401Unauthorized,
            detail = exception.Message
        });
    }

    private static async Task HandleForbiddenExceptionAsync(HttpContext context, ForbiddenException exception)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Forbidden",
            status = StatusCodes.Status403Forbidden,
            detail = exception.Message
        });
    }

    private static async Task HandleNotFoundExceptionAsync(HttpContext context, NotFoundException exception)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Not Found",
            status = StatusCodes.Status404NotFound,
            detail = exception.Message
        });
    }

    private static async Task HandleConflictExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Conflict",
            status = StatusCodes.Status409Conflict,
            detail = exception.Message
        });
    }

    private static async Task HandleExceptionAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = ErrorMessages.UnexpectedError
        });
    }
}
