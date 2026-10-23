namespace TaskFlow.Service.Authorization;

public static class Permissions
{
    public const string ClaimType = "permission";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string ProjectsManage = "projects.manage";
    public const string ReportsView = "reports.view";

    public static readonly string[] All =
    [
        UsersManage,
        RolesManage,
        ProjectsManage,
        ReportsView
    ];
}
