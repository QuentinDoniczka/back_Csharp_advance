namespace BackBase.Application.Commands.Register;

using MediatR;

public record RegisterCommand(string Email, string Password) : IRequest<RegisterResult>;
