namespace BackBase.Application.Commands.DeactivateAccount;

using MediatR;

public record DeactivateAccountCommand(Guid UserId) : IRequest;
