namespace BackBase.Application.Queries.GetMyProfile;

using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, UserProfileOutput>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public GetMyProfileQueryHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<UserProfileOutput> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile is null)
            throw new NotFoundException(AuthErrorMessages.ProfileNotFound);

        return UserProfileOutput.FromEntity(profile);
    }
}
