using System.Security.Claims;
using BackBase.Application.Commands.SendChatMessage;
using BackBase.Application.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackBase.Infrastructure.Chat;

[Authorize]
public sealed class ChatHub : Hub
{
    private const string UserIdentityNotFound = "User identity not found.";
    private const string InvalidUserIdFormat = "User ID claim is not a valid GUID.";
    private const string UserEmailNotFound = "User email not found.";

    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatConstants.GlobalChatGroup)
            .ConfigureAwait(false);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    public async Task JoinGlobalChat()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ChatConstants.GlobalChatGroup)
            .ConfigureAwait(false);
    }

    public async Task LeaveGlobalChat()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatConstants.GlobalChatGroup)
            .ConfigureAwait(false);
    }

    public async Task SendMessage(string message)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException(UserIdentityNotFound);

        var email = Context.User?.FindFirstValue(ClaimTypes.Email)
            ?? throw new HubException(UserEmailNotFound);

        if (!Guid.TryParse(userId, out var senderUserId))
            throw new HubException(InvalidUserIdFormat);

        var command = new SendChatMessageCommand(
            senderUserId,
            email,
            message);

        await _mediator.Send(command).ConfigureAwait(false);
    }
}
