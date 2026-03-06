namespace BackBase.Application.Commands.ChangePassword;

using BackBase.Application.Interfaces;
using MediatR;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        await _identityService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken).ConfigureAwait(false);
    }
}
