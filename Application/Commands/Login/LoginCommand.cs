namespace BackBase.Application.Commands.Login;

using BackBase.Application.DTOs.Output;
using MediatR;

public record LoginCommand(string Email, string Password) : IRequest<AuthTokenResult>;
