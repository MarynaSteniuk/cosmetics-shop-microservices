namespace Auth.Api.Data;

public static class AppRoles
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [User, Admin];
}
