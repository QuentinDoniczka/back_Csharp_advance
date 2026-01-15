using MediatR;
using Microsoft.AspNetCore.Mvc;
using BackBase.Application.Commands.Register;
using BackBase.API.DTOs;

namespace BackBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var command = new RegisterCommand(
            Email: request.Email,
            Password: request.Password,
            FirstName: request.FirstName,
            LastName: request.LastName
        );

        var result = await _mediator.Send(command);

        var response = new RegisterResponseDto
        {
            Id = result.Id,
            Email = result.Email,
            FullName = result.FullName
        };

        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        return Ok();
    }
}