namespace BackBase.Application.Queries.GetUserRole;

using BackBase.Application.Constants;
using BackBase.Application.DTOs.Output;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Constants;
using MediatR;

public sealed class GetUserRoleQueryHandler : IRequestHandler<GetUserRoleQuery, UserRoleOutput>
{
    private readonly IIdentityService _identityService;

    public GetUserRoleQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<UserRoleOutput> Handle(GetUserRoleQuery request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindByIdAsync(request.TargetUserId, cancellationToken).ConfigureAwait(false);

        if (user is null)
            throw new NotFoundException(AuthErrorMessages.UserNotFound);

        var roles = await _identityService.GetRolesAsync(request.TargetUserId, cancellationToken).ConfigureAwait(false);
        var role = roles.Count > 0 ? roles[0] : AppRoles.Member;

        return new UserRoleOutput(request.TargetUserId, role);
    }
}
