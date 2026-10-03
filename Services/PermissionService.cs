using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Services;

namespace ZDLISPlus.Web.Services;

public class PermissionService
{
    private readonly LabDbContext _db;
    private readonly UserManager<IdentityUser> _users;

    public PermissionService(LabDbContext db, UserManager<IdentityUser> users)
    {
        _db = db;
        _users = users;
    }

    /// <summary>
    /// True if the principal is in Administrator, OR one of the principal's
    /// roles has a RolePermission row for <paramref name="moduleKey"/>.
    /// </summary>
    public async Task<bool> CanAccessAsync(ClaimsPrincipal user, string moduleKey)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole("Administrator")) return true;

        var name = user.Identity.Name;
        if (string.IsNullOrWhiteSpace(name)) return false;

        var u = await _users.FindByNameAsync(name);
        if (u is null) return false;

        // Get role NAMES for the user.
        var roleNames = await _users.GetRolesAsync(u);
        if (roleNames.Count == 0) return false;

        // Map role names → role IDs (RolePermission stores RoleId, not name).
        var roleIds = await _db.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        if (roleIds.Count == 0) return false;

        return await _db.RolePermissions
            .AnyAsync(p => roleIds.Contains(p.RoleId) && p.ModuleKey == moduleKey);
    }

    /// <summary>
    /// All module keys the principal is allowed to see. Admin = all.
    /// </summary>
    public async Task<HashSet<string>> GetGrantedModulesAsync(ClaimsPrincipal user)
    {
        var granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (user.Identity?.IsAuthenticated != true) return granted;

        if (user.IsInRole("Administrator"))
        {
            foreach (var m in AppModules.All) granted.Add(m.Key);
            return granted;
        }

        var name = user.Identity.Name;
        if (string.IsNullOrWhiteSpace(name)) return granted;

        var u = await _users.FindByNameAsync(name);
        if (u is null) return granted;

        var roleNames = await _users.GetRolesAsync(u);
        if (roleNames.Count == 0) return granted;

        var roleIds = await _db.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        if (roleIds.Count == 0) return granted;

        var keys = await _db.RolePermissions
            .Where(p => roleIds.Contains(p.RoleId))
            .Select(p => p.ModuleKey)
            .Distinct()
            .ToListAsync();

        foreach (var k in keys) granted.Add(k);
        return granted;
    }

    /// <summary>Set the exact set of modules for a role.</summary>
    public async Task SetRolePermissionsAsync(string roleId, IEnumerable<string> moduleKeys)
    {
        var existing = await _db.RolePermissions
            .Where(p => p.RoleId == roleId)
            .ToListAsync();

        var wanted = new HashSet<string>(moduleKeys, StringComparer.OrdinalIgnoreCase);

        _db.RolePermissions.RemoveRange(existing.Where(p => !wanted.Contains(p.ModuleKey)));

        var have = existing.Select(p => p.ModuleKey)
                           .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var key in wanted.Except(have))
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, ModuleKey = key });

        await _db.SaveChangesAsync();
    }
}