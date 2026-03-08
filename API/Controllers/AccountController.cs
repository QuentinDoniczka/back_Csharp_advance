namespace BackBase.API.Controllers;

using BackBase.API.Authorization;
using BackBase.API.DTOs;
using BackBase.API.Extensions;
using BackBase.Application.Commands.ChangePassword;
using BackBase.Application.Commands.DeactivateAccount;
using BackBase.Application.Commands.ReactivateAccount;
using BackBase.Application.Commands.UpdateProfile;
using BackBase.Application.Queries.GetMyProfile;
using BackBase.Application.Queries.GetUserProfile;
using BackBase.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponseDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var query = new GetMyProfileQuery(userId.Value);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(UserProfileResponseDto.FromOutput(result));
    }

    [Authorize]
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserProfileResponseDto>> GetUserProfile(Guid userId, CancellationToken cancellationToken)
    {
        var query = new GetUserProfileQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(UserProfileResponseDto.FromOutput(result));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponseDto>> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var command = new UpdateProfileCommand(userId.Value, request.DisplayName, request.AvatarUrl);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(UserProfileResponseDto.FromOutput(result));
    }

    [Authorize]
    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var command = new ChangePasswordCommand(userId.Value, request.CurrentPassword, request.NewPassword);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("me/deactivate")]
    public async Task<IActionResult> DeactivateAccount(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var command = new DeactivateAccountCommand(userId.Value);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [MinimumRole(RoleLevel.Admin)]
    [HttpPost("{userId:guid}/reactivate")]
    public async Task<IActionResult> ReactivateAccount(Guid userId, CancellationToken cancellationToken)
    {
        var command = new ReactivateAccountCommand(userId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
