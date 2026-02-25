namespace BackBase.Application.Commands.Register;

using BackBase.Application.Constants;
using BackBase.Application.Interfaces;
using MediatR;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.RegisterAsync(request.Email, request.Password, cancellationToken).ConfigureAwait(false);
        await _identityService.AssignRoleAsync(user.UserId, AppRoles.Player, cancellationToken).ConfigureAwait(false);
        return new RegisterResult(user.UserId, user.Email);
    }
}
