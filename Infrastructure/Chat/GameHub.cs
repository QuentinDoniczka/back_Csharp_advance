using System.Security.Claims;
using BackBase.Application.Commands.SendChatMessage;
using BackBase.Application.Constants;
using BackBase.Application.Helpers;
using BackBase.Application.Interfaces;
using BackBase.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackBase.Infrastructure.Chat;

[Authorize]
public sealed class GameHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IChannelAuthorizationService _channelAuthorizationService;

    public GameHub(IMediator mediator, IChannelAuthorizationService channelAuthorizationService)
    {
        _mediator = mediator;
        _channelAuthorizationService = channelAuthorizationService;
    }

    public async Task JoinChannel(ChannelType channelType, string channelId)
    {
        ValidateChannelId(channelId);

        var userId = GetUserId();
        var isAuthorized = await _channelAuthorizationService
            .CanUserAccessChannelAsync(userId, channelType, channelId)
            .ConfigureAwait(false);

        if (!isAuthorized)
            throw new HubException(ChannelConstants.ChannelAccessDenied);

        var channelName = ChannelNameBuilder.Build(channelType, channelId);
        await Groups.AddToGroupAsync(Context.ConnectionId, channelName)
            .ConfigureAwait(false);
    }

    public async Task LeaveChannel(ChannelType channelType, string channelId)
    {
        ValidateChannelId(channelId);

        var channelName = ChannelNameBuilder.Build(channelType, channelId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelName)
            .ConfigureAwait(false);
    }

    public async Task SendMessage(ChannelType channelType, string channelId, string message)
    {
        ValidateChannelId(channelId);

        var userId = GetUserId();

        var isAuthorized = await _channelAuthorizationService
            .CanUserAccessChannelAsync(userId, channelType, channelId)
            .ConfigureAwait(false);

        if (!isAuthorized)
            throw new HubException(ChannelConstants.ChannelAccessDenied);

        var email = Context.User?.FindFirstValue(ClaimTypes.Email)
            ?? throw new HubException(ChatConstants.UserEmailNotFound);

        var channelName = ChannelNameBuilder.Build(channelType, channelId);

        var command = new SendChatMessageCommand(
            userId,
            email,
            channelName,
            message);

        await _mediator.Send(command).ConfigureAwait(false);
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException(ChatConstants.UserIdentityNotFound);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new HubException(ChatConstants.InvalidUserIdFormat);

        return userId;
    }

    private static void ValidateChannelId(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            throw new HubException(ChannelConstants.ChannelIdEmpty);

        if (channelId.Length > ChannelConstants.ChannelIdMaxLength)
            throw new HubException(ChannelConstants.ChannelIdTooLong);
    }
}
