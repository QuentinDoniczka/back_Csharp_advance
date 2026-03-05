using System.Security.Claims;
using BackBase.Application.Commands.SendChatMessage;
using BackBase.Application.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackBase.Infrastructure.Chat;

[Authorize]
public sealed class GameHub : Hub
{
    private readonly IMediator _mediator;

    public GameHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task JoinSalon(string salonName)
    {
        ValidateSalonName(salonName);
        await Groups.AddToGroupAsync(Context.ConnectionId, salonName)
            .ConfigureAwait(false);
    }

    public async Task LeaveSalon(string salonName)
    {
        ValidateSalonName(salonName);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, salonName)
            .ConfigureAwait(false);
    }

    public async Task SendMessage(string salonName, string message)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException(ChatConstants.UserIdentityNotFound);

        var email = Context.User?.FindFirstValue(ClaimTypes.Email)
            ?? throw new HubException(ChatConstants.UserEmailNotFound);

        if (!Guid.TryParse(userId, out var senderUserId))
            throw new HubException(ChatConstants.InvalidUserIdFormat);

        var command = new SendChatMessageCommand(
            senderUserId,
            email,
            salonName,
            message);

        await _mediator.Send(command).ConfigureAwait(false);
    }

    private static void ValidateSalonName(string salonName)
    {
        if (string.IsNullOrWhiteSpace(salonName))
            throw new HubException(ChatConstants.SalonNameEmpty);

        if (salonName.Length > ChatConstants.SalonNameMaxLength)
            throw new HubException(ChatConstants.SalonNameTooLong);
    }
}
