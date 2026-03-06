namespace BackBase.Application.Commands.ReactivateAccount;

using BackBase.Application.Constants;
using BackBase.Application.Exceptions;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class ReactivateAccountCommandHandler : IRequestHandler<ReactivateAccountCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public ReactivateAccountCommandHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task Handle(ReactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken).ConfigureAwait(false);

        if (profile is null)
            throw new NotFoundException(AuthErrorMessages.ProfileNotFound);

        if (!profile.IsDeactivated)
            throw new ConflictException(AuthErrorMessages.AccountNotDeactivated);

        profile.Reactivate();
        await _userProfileRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
    }
}
