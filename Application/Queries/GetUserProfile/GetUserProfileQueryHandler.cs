namespace BackBase.Application.Queries.GetUserProfile;

using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Domain.Interfaces;
using MediatR;

public sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileOutput>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public GetUserProfileQueryHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<UserProfileOutput> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile is null)
            throw new NotFoundException(AuthErrorMessages.ProfileNotFound);

        return UserProfileOutput.FromEntity(profile);
    }
}
