using BackBase.Domain.Enums;

namespace BackBase.Domain.Constants;

public static class AppRoles
{
    public const string Member = nameof(RoleLevel.Member);
    public const string Admin = nameof(RoleLevel.Admin);
    public const string SuperAdmin = nameof(RoleLevel.SuperAdmin);

    public static readonly IReadOnlyList<string> All =
        Enum.GetValues<RoleLevel>().Select(r => r.ToString()).ToList().AsReadOnly();
}
