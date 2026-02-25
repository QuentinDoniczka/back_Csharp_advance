using BackBase.Application.Constants;
using Microsoft.AspNetCore.Authorization;

namespace BackBase.Application.Authorization;

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
        yield return new MinimumRoleRequirement(MinimumLevel);
    }
}
