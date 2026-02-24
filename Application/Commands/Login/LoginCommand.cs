namespace BackBase.Application.Commands.Login;

using MediatR;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;
