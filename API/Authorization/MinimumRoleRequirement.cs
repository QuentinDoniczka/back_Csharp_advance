using BackBase.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace BackBase.API.Authorization;

public sealed record MinimumRoleRequirement(RoleLevel MinimumLevel) : IAuthorizationRequirement;
