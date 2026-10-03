namespace ZDLISPlus.Web.Models;

public class RolePermission
{
    public int Id { get; set; }
    public string RoleId { get; set; } = "";
    public string ModuleKey { get; set; } = "";
}