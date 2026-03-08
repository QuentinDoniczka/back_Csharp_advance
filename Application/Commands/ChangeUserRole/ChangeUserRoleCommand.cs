namespace BackBase.Application.Commands.ChangeUserRole;

using MediatR;

public record ChangeUserRoleCommand(Guid CallerUserId, Guid TargetUserId, string NewRole) : IRequest;
