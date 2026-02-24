namespace BackBase.API.Controllers;

using BackBase.API.DTOs;
using BackBase.Application.Commands.Login;
using BackBase.Application.Commands.RefreshToken;
using BackBase.Application.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new RegisterResponseDto(result.UserId, result.Email));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new LoginResponseDto(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAt));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new RefreshTokenResponseDto(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAt));
    }
}
