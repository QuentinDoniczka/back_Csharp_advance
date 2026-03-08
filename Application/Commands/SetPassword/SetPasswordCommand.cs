namespace BackBase.Application.Commands.SetPassword;

using MediatR;

public record SetPasswordCommand(Guid UserId, string Password) : IRequest;
