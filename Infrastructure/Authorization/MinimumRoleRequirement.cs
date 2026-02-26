using BackBase.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace BackBase.Infrastructure.Authorization;

public sealed record MinimumRoleRequirement(RoleLevel MinimumLevel) : IAuthorizationRequirement;
