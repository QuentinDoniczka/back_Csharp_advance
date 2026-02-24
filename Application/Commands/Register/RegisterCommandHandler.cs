namespace BackBase.Application.Commands.Register;

using BackBase.Application.Interfaces;
using MediatR;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IAuthenticationService _authenticationService;

    public RegisterCommandHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return await _authenticationService.RegisterAsync(request.Email, request.Password, cancellationToken).ConfigureAwait(false);
    }
}
