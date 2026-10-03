using System.ComponentModel.DataAnnotations;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Models.ViewModels;

public class RoleRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int UserCount { get; set; }
    public int ModuleCount { get; set; }
    public bool IsSystemRole { get; set; }
    public bool HasUsers => UserCount > 0;
}

public class RoleListViewModel
{
    public List<RoleRow> Rows { get; set; } = new();
    public int TotalModules { get; set; }
}

public class RoleEditViewModel
{
    public string? Id { get; set; }

    [Required, Display(Name = "Role name")]
    [StringLength(64, MinimumLength = 2)]
    public string Name { get; set; } = "";

    public int UserCount { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsAdministrator { get; set; }

    public List<string> GrantedModules { get; set; } = new();
    public List<ModuleDef> AvailableModules { get; set; } = new();
}