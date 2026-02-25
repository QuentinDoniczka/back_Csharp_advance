namespace BackBase.Application.Constants;

public static class AppRoles
{
    public const string Player = "Player";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly IReadOnlyList<string> All = [Player, Admin, SuperAdmin];
}
