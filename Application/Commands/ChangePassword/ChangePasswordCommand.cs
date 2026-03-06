namespace BackBase.Application.Commands.ChangePassword;

using MediatR;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;
