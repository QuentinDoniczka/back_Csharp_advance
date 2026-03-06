namespace BackBase.Application.Commands.ReactivateAccount;

using MediatR;

public record ReactivateAccountCommand(Guid TargetUserId) : IRequest;
