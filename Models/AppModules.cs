namespace ZDLISPlus.Web.Models;

/// <summary>
/// Canonical list of permission-gated modules. The Key matches
/// the controller name (or a stable synonym), and is what
/// [ModuleAuthorize("Patients")] checks against.
/// </summary>
public static class AppModules
{
    public static readonly IReadOnlyList<ModuleDef> All = new List<ModuleDef>
    {
        new("Home",          "Dashboard",      "Overview of the laboratory"),
        new("Patients",      "Patients",       "Patient registry"),
        new("Orders",        "Lab Orders",     "Order creation and tracking"),
        new("Results",       "Results",        "Result entry and printing"),
        new("Validation",    "Validation",     "Validate and release results"),
        new("Reports",       "Reports",        "Analytics and workload"),
        new("Instruments",   "Instruments",    "Instrument status"),
        new("Audit",         "Audit Trail",    "Audit log"),
        new("Configuration", "Configuration",  "Test catalog"),
        new("Users",         "User Accounts",  "User administration"),
        new("Roles",         "Roles",          "Role administration"),
    };

    public static ModuleDef? Find(string key) =>
        All.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));
}

public record ModuleDef(string Key, string Title, string Description);