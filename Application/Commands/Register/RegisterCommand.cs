using MediatR;

namespace BackBase.Application.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<RegisterResult>;
