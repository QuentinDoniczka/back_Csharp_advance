using BackBase.Application.Constants;

namespace BackBase.Application.Authorization;

public static class RoleHierarchy
{
    private static readonly Dictionary<string, RoleLevel> RoleLevels =
        Enum.GetValues<RoleLevel>()
            .ToDictionary(level => level.ToString(), level => level, StringComparer.OrdinalIgnoreCase);

    public static bool TryGetLevel(string roleName, out RoleLevel level)
    {
        return RoleLevels.TryGetValue(roleName, out level);
    }
}
