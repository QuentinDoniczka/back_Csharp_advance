namespace BackBase.API.Controllers;

using BackBase.API.Authorization;
using BackBase.API.DTOs;
using BackBase.API.Extensions;
using BackBase.Application.Commands.ChangeUserRole;
using BackBase.Application.Queries.GetUserRole;
using BackBase.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/roles")]
public sealed class RoleController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [MinimumRole(RoleLevel.Admin)]
    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeUserRoleRequestDto request, CancellationToken cancellationToken)
    {
        var callerUserId = User.GetUserId();
        if (callerUserId is null)
            return Unauthorized();

        var command = new ChangeUserRoleCommand(callerUserId.Value, userId, request.Role);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [MinimumRole(RoleLevel.Admin)]
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserRoleResponseDto>> GetUserRole(Guid userId, CancellationToken cancellationToken)
    {
        var query = new GetUserRoleQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new UserRoleResponseDto(result.UserId, result.Role));
    }
}
