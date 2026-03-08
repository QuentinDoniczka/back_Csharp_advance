namespace BackBase.Application.Commands.SetPassword;

using BackBase.Application.Interfaces;
using MediatR;

public sealed class SetPasswordCommandHandler : IRequestHandler<SetPasswordCommand>
{
    private readonly IIdentityService _identityService;

    public SetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(SetPasswordCommand request, CancellationToken cancellationToken)
    {
        await _identityService.SetPasswordAsync(request.UserId, request.Password, cancellationToken).ConfigureAwait(false);
    }
}
