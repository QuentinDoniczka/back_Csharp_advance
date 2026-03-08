namespace BackBase.Application.Commands.UpdateProfile;

using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileOutput>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public UpdateProfileCommandHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<UserProfileOutput> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile is null)
            throw new NotFoundException(AuthErrorMessages.ProfileNotFound);

        profile.Update(request.DisplayName, request.AvatarUrl);
        await _userProfileRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);

        return UserProfileOutput.FromEntity(profile);
    }
}
