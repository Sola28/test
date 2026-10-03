using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Models.ViewModels;
using ZDLISPlus.Web.Services;
using ZDLISPlus.Web.Authorization;

namespace ZDLISPlus.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class RolesController : Controller
{
    private static readonly HashSet<string> SystemRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Administrator", "Technologist", "Pathologist", "Reception"
    };

    private readonly RoleManager<IdentityRole> _roles;
    private readonly UserManager<IdentityUser> _users;
    private readonly AuditService _audit;
    private readonly LabDbContext _db;

    public RolesController(
        RoleManager<IdentityRole> roles,
        UserManager<IdentityUser> users,
        AuditService audit,
        LabDbContext db)
    {
        _roles = roles;
        _users = users;
        _audit = audit;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roles.Roles.OrderBy(r => r.Name).ToListAsync();
        var rows = new List<RoleRow>();

        foreach (var r in roles)
        {
            var users = await _users.GetUsersInRoleAsync(r.Name!);
            var permCount = string.Equals(r.Name, "Administrator", StringComparison.OrdinalIgnoreCase)
                ? AppModules.All.Count
                : await _db.RolePermissions.CountAsync(p => p.RoleId == r.Id);

            rows.Add(new RoleRow
            {
                Id = r.Id,
                Name = r.Name ?? "",
                UserCount = users.Count,
                IsSystemRole = SystemRoles.Contains(r.Name ?? ""),
                ModuleCount = permCount
            });
        }

        return View(new RoleListViewModel { Rows = rows });
    }

    [HttpGet]
    public IActionResult Create() => View(new RoleEditViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleEditViewModel vm)
    {
        var name = vm.Name.Trim();
        if (await _roles.RoleExistsAsync(name))
            ModelState.AddModelError(nameof(vm.Name), "A role with that name already exists.");

        if (!ModelState.IsValid) return View(vm);

        var result = await _roles.CreateAsync(new IdentityRole(name));
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(vm);
        }

        await _audit.LogAsync("RoleCreated", "Role", name, "New role created");
        TempData["Success"] = $"Role {name} created. Now choose its module access.";
        return RedirectToAction(nameof(Edit), new { id = (await _roles.FindByNameAsync(name))!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roles.FindByIdAsync(id);
        if (role is null) return NotFound();

        var users = await _users.GetUsersInRoleAsync(role.Name!);
        var granted = await _db.RolePermissions
            .Where(p => p.RoleId == role.Id)
            .Select(p => p.ModuleKey)
            .ToListAsync();

        return View(new RoleEditViewModel
        {
            Id = role.Id,
            Name = role.Name ?? "",
            UserCount = users.Count,
            IsSystemRole = SystemRoles.Contains(role.Name ?? ""),
            IsAdministrator = string.Equals(role.Name, "Administrator", StringComparison.OrdinalIgnoreCase),
            GrantedModules = granted,
            AvailableModules = AppModules.All.ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleEditViewModel vm)
    {
        if (vm.Id is null) return BadRequest();

        var role = await _roles.FindByIdAsync(vm.Id);
        if (role is null) return NotFound();

        var oldName = role.Name ?? "";
        var newName = vm.Name.Trim();
        vm.IsSystemRole = SystemRoles.Contains(oldName);
        vm.IsAdministrator = string.Equals(oldName, "Administrator", StringComparison.OrdinalIgnoreCase);

        // --- Name validation ---
        if (vm.IsSystemRole && !string.Equals(oldName, newName, StringComparison.Ordinal))
            ModelState.AddModelError(nameof(vm.Name), $"{oldName} is a system role and cannot be renamed.");

        if (!string.Equals(oldName, newName, StringComparison.Ordinal)
            && await _roles.RoleExistsAsync(newName))
            ModelState.AddModelError(nameof(vm.Name), "A role with that name already exists.");

        if (!ModelState.IsValid)
        {
            vm.AvailableModules = AppModules.All.ToList();
            vm.UserCount = (await _users.GetUsersInRoleAsync(oldName)).Count;
            return View(vm);
        }

        // --- Rename (custom roles only) ---
        if (!string.Equals(oldName, newName, StringComparison.Ordinal))
        {
            role.Name = newName;
            var result = await _roles.UpdateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                vm.AvailableModules = AppModules.All.ToList();
                vm.UserCount = (await _users.GetUsersInRoleAsync(oldName)).Count;
                return View(vm);
            }
            await _audit.LogAsync("RoleRenamed", "Role", newName, $"Renamed from {oldName}");
        }

        // --- Module permissions ---
        if (vm.IsAdministrator)
        {
            // Administrator always has all modules — keep DB in sync anyway.
            var allKeys = AppModules.All.Select(m => m.Key);
            var svc = HttpContext.RequestServices.GetRequiredService<PermissionService>();
            await svc.SetRolePermissionsAsync(role.Id, allKeys);
        }
        else
        {
            var svc = HttpContext.RequestServices.GetRequiredService<PermissionService>();
            await svc.SetRolePermissionsAsync(role.Id, vm.GrantedModules);

            var summary = string.Join(", ", vm.GrantedModules.OrderBy(x => x));
            await _audit.LogAsync("RolePermissionsChanged", "Role", newName,
                $"Modules: {summary}");
        }

        TempData["Success"] = $"Role {newName} saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roles.FindByIdAsync(id);
        if (role is null) return NotFound();

        var name = role.Name ?? "";

        if (SystemRoles.Contains(name))
        {
            TempData["Error"] = $"{name} is a system role and cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        var users = await _users.GetUsersInRoleAsync(name);
        if (users.Count > 0)
        {
            TempData["Error"] = $"Cannot delete {name} — it is assigned to {users.Count} user(s). Remove the role from those users first.";
            return RedirectToAction(nameof(Index));
        }

        // Remove permission rows too
        var perms = await _db.RolePermissions.Where(p => p.RoleId == role.Id).ToListAsync();
        _db.RolePermissions.RemoveRange(perms);

        var result = await _roles.DeleteAsync(role);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _audit.LogAsync("RoleDeleted", "Role", name, "Role removed");
        TempData["Success"] = $"Role {name} deleted.";
        return RedirectToAction(nameof(Index));
    }
}