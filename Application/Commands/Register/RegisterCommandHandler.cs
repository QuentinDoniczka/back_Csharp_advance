namespace BackBase.Application.Commands.Register;

using BackBase.Application.Constants;
using BackBase.Domain.Constants;
using BackBase.Domain.Entities;
using BackBase.Domain.Interfaces;
using BackBase.Application.Interfaces;
using MediatR;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IIdentityService _identityService;
    private readonly IUserProfileRepository _userProfileRepository;

    public RegisterCommandHandler(IIdentityService identityService, IUserProfileRepository userProfileRepository)
    {
        _identityService = identityService;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.RegisterAsync(request.Email, request.Password, cancellationToken).ConfigureAwait(false);
        await _identityService.AssignRoleAsync(user.UserId, AppRoles.Member, cancellationToken).ConfigureAwait(false);

        var displayName = user.Email.Split('@')[0];
        if (displayName.Length > ProfileConstants.DisplayNameMaxLength)
            displayName = displayName[..ProfileConstants.DisplayNameMaxLength];

        var profile = UserProfile.Create(user.UserId, displayName);
        await _userProfileRepository.AddAsync(profile, cancellationToken).ConfigureAwait(false);

        return new RegisterResult(user.UserId, user.Email);
    }
}
