namespace ZDLISPlus.Web.Models.ViewModels;

public class UserRow
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsLockedOut { get; set; }
    public bool IsCurrentUser { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserListViewModel
{
    public string? Q { get; set; }
    public string? Role { get; set; }
    public List<UserRow> Rows { get; set; } = new();
    public List<string> AllRoles { get; set; } = new();
}