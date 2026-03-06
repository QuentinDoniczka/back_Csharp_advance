namespace BackBase.Application.Commands.ChangeUserRole;

using BackBase.Application.Authorization;
using BackBase.Application.Constants;
using BackBase.Application.Exceptions;
using BackBase.Application.Interfaces;
using BackBase.Domain.Constants;
using BackBase.Domain.Enums;
using MediatR;

public sealed class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand>
{
    private readonly IIdentityService _identityService;

    public ChangeUserRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.CallerUserId == request.TargetUserId)
            throw new ForbiddenException(AuthErrorMessages.CannotChangeOwnRole);

        if (!RoleHierarchy.TryGetLevel(request.NewRole, out var newRoleLevel))
            throw new ForbiddenException(AuthErrorMessages.InvalidRole);

        var targetUser = await _identityService.FindByIdAsync(request.TargetUserId, cancellationToken).ConfigureAwait(false);

        if (targetUser is null)
            throw new NotFoundException(AuthErrorMessages.UserNotFound);

        var callerLevel = await GetHighestRoleLevelAsync(request.CallerUserId, cancellationToken).ConfigureAwait(false);
        var targetLevel = await GetHighestRoleLevelAsync(request.TargetUserId, cancellationToken).ConfigureAwait(false);

        if (callerLevel <= targetLevel || callerLevel <= newRoleLevel)
            throw new ForbiddenException(AuthErrorMessages.InsufficientRoleLevel);

        if (targetLevel == RoleLevel.SuperAdmin)
        {
            var superAdminCount = await _identityService.CountUsersInRoleAsync(AppRoles.SuperAdmin, cancellationToken).ConfigureAwait(false);
            if (superAdminCount <= 1)
                throw new ForbiddenException(AuthErrorMessages.CannotDemoteLastSuperAdmin);
        }

        await _identityService.ReplaceRoleAsync(request.TargetUserId, request.NewRole, cancellationToken).ConfigureAwait(false);
    }

    private async Task<RoleLevel> GetHighestRoleLevelAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await _identityService.GetRolesAsync(userId, cancellationToken).ConfigureAwait(false);
        var highestLevel = RoleLevel.Member;

        foreach (var role in roles)
        {
            if (RoleHierarchy.TryGetLevel(role, out var level) && level > highestLevel)
                highestLevel = level;
        }

        return highestLevel;
    }
}
