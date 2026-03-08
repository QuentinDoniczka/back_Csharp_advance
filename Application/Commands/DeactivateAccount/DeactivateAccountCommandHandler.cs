namespace BackBase.Application.Commands.DeactivateAccount;

using BackBase.Application.Constants;
using BackBase.Application.Exceptions;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class DeactivateAccountCommandHandler : IRequestHandler<DeactivateAccountCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public DeactivateAccountCommandHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile is null)
            throw new NotFoundException(AuthErrorMessages.ProfileNotFound);

        if (profile.IsDeactivated)
            throw new ConflictException(AuthErrorMessages.AccountAlreadyDeactivated);

        profile.Deactivate();
        await _userProfileRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
    }
}
