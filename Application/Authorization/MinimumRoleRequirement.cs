using BackBase.Application.Constants;
using Microsoft.AspNetCore.Authorization;

namespace BackBase.Application.Authorization;

public sealed record MinimumRoleRequirement(RoleLevel MinimumLevel) : IAuthorizationRequirement;
