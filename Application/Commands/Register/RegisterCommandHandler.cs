using MediatR;

namespace BackBase.Application.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();
        var fullName = $"{request.FirstName} {request.LastName}";

        await Task.CompletedTask;

        return new RegisterResult(userId, request.Email, fullName);
    }
}
