using BackBase.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace BackBase.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class MinimumRoleAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    public RoleLevel MinimumLevel { get; }

    public MinimumRoleAttribute(RoleLevel minimumLevel)
    {
        MinimumLevel = minimumLevel;
    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new DenyAnonymousAuthorizationRequirement();
        yield return new MinimumRoleRequirement(MinimumLevel);
    }
}
