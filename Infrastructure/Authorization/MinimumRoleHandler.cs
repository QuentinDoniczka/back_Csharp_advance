using System.Security.Claims;
using BackBase.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace BackBase.Infrastructure.Authorization;

public sealed class MinimumRoleHandler : AuthorizationHandler<MinimumRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumRoleRequirement requirement)
    {
        var roleClaims = context.User.FindAll(ClaimTypes.Role);

        foreach (var claim in roleClaims)
        {
            if (RoleHierarchy.TryGetLevel(claim.Value, out var level) && level >= requirement.MinimumLevel)
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
