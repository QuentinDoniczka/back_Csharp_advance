using System.Text.Json;
using BackBase.Application.Constants;
using BackBase.Application.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BackBase.Infrastructure.Chat;

public sealed class HubExceptionFilter : IHubFilter
{
    private static readonly JsonSerializerOptions JsonWebOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<HubExceptionFilter> _logger;

    public HubExceptionFilter(ILogger<HubExceptionFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            var message = JsonSerializer.Serialize(ex.Errors, JsonWebOptions);
            throw new HubException(message);
        }
        catch (AuthenticationException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in hub method {Method}", invocationContext.HubMethodName);
            throw new HubException(ErrorMessages.UnexpectedError);
        }
    }
}
