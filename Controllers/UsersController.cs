using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Models.ViewModels;
using ZDLISPlus.Web.Services;

namespace ZDLISPlus.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly AuditService _audit;

    public UsersController(
        UserManager<IdentityUser> users,
        RoleManager<IdentityRole> roles,
        AuditService audit)
    {
        _users = users;
        _roles = roles;
        _audit = audit;
    }

    // ---------------- LIST ----------------
    public async Task<IActionResult> Index(string? q, string? role)
    {
        var query = _users.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(q)) ||
                (u.UserName != null && u.UserName.Contains(q)));

        var users = await query.OrderBy(u => u.Email).ToListAsync();

        var rows = new List<UserRow>();
        foreach (var u in users)
        {
            var roles = await _users.GetRolesAsync(u);
            if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role))
                continue;

            rows.Add(new UserRow
            {
                Id = u.Id,
                Email = u.Email ?? u.UserName ?? "",
                DisplayName = u.UserName ?? u.Email ?? "",
                IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                IsCurrentUser = string.Equals(u.UserName, User.Identity?.Name, StringComparison.OrdinalIgnoreCase),
                Roles = roles.ToList()
            });
        }

        return View(new UserListViewModel
        {
            Q = q,
            Role = role,
            Rows = rows,
            AllRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync()
        });
    }

    // ---------------- CREATE ----------------
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new UserCreateViewModel
        {
            AvailableRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
            return View(vm);
        }

        var user = new IdentityUser
        {
            UserName = vm.Email.Trim(),
            Email = vm.Email.Trim(),
            EmailConfirmed = true
        };

        var result = await _users.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            vm.AvailableRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
            return View(vm);
        }

        if (vm.SelectedRoles.Count > 0)
            await _users.AddToRolesAsync(user, vm.SelectedRoles);

        await _audit.LogAsync("UserCreated", "User", user.UserName!,
            $"Roles: {string.Join(", ", vm.SelectedRoles)}");

        TempData["Success"] = $"User {user.UserName} created.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- EDIT ----------------
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var vm = new UserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            DisplayName = user.UserName ?? "",
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            IsCurrentUser = string.Equals(user.UserName, User.Identity?.Name, StringComparison.OrdinalIgnoreCase),
            SelectedRoles = (await _users.GetRolesAsync(user)).ToList(),
            AvailableRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync()
        };

        vm.IsLastAdmin = await IsLastAdminAsync(user);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel vm)
    {
        var user = await _users.FindByIdAsync(vm.Id);
        if (user is null) return NotFound();

        vm.IsCurrentUser = string.Equals(user.UserName, User.Identity?.Name, StringComparison.OrdinalIgnoreCase);
        vm.IsLastAdmin = await IsLastAdminAsync(user);

        // Guard: last admin cannot lose Administrator role.
        var isAdminNow = await _users.IsInRoleAsync(user, "Administrator");
        var willBeAdmin = vm.SelectedRoles.Contains("Administrator");

        if (vm.IsLastAdmin && isAdminNow && !willBeAdmin)
            ModelState.AddModelError(nameof(vm.SelectedRoles),
                "Cannot remove Administrator from the last remaining admin.");

        if (!ModelState.IsValid)
        {
            vm.AvailableRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
            vm.IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
            return View(vm);
        }

        // ---------- Email / username change ----------
        var newEmail = vm.Email.Trim();
        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = newEmail;
            user.UserName = newEmail;
            user.NormalizedEmail = newEmail.ToUpperInvariant();
            user.NormalizedUserName = newEmail.ToUpperInvariant();
            var upd = await _users.UpdateAsync(user);
            if (!upd.Succeeded)
            {
                foreach (var e in upd.Errors) ModelState.AddModelError("", e.Description);
                vm.AvailableRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
                return View(vm);
            }
        }

        // ---------- Roles diff ----------
        var currentRoles = (await _users.GetRolesAsync(user)).ToList();
        var toAdd = vm.SelectedRoles.Except(currentRoles).ToList();
        var toRemove = currentRoles.Except(vm.SelectedRoles).ToList();

        if (toRemove.Count > 0) await _users.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Count > 0) await _users.AddToRolesAsync(user, toAdd);

        // ---------- Password reset (optional) ----------
        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var reset = await _users.ResetPasswordAsync(user, token, vm.NewPassword);
            if (!reset.Succeeded)
            {
                foreach (var e in reset.Errors) ModelState.AddModelError("", e.Description);
                vm.AvailableRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
                return View(vm);
            }
            await _audit.LogAsync("PasswordReset", "User", user.UserName!, "Password reset by admin");
        }

        await _audit.LogAsync("UserUpdated", "User", user.UserName!,
            $"Roles: {string.Join(", ", vm.SelectedRoles)}");

        TempData["Success"] = $"User {user.UserName} updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- LOCK / UNLOCK ----------------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (string.Equals(user.UserName, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "You cannot lock your own account.";
            return RedirectToAction(nameof(Index));
        }

        if (await IsLastAdminAsync(user) && await _users.IsInRoleAsync(user, "Administrator"))
        {
            TempData["Error"] = "Cannot lock the last remaining Administrator.";
            return RedirectToAction(nameof(Index));
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            await _users.SetLockoutEndDateAsync(user, null);
            await _audit.LogAsync("UserUnlocked", "User", user.UserName!, "Account unlocked");
            TempData["Success"] = $"User {user.UserName} unlocked.";
        }
        else
        {
            await _users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            await _audit.LogAsync("UserLocked", "User", user.UserName!, "Account deactivated");
            TempData["Success"] = $"User {user.UserName} deactivated.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------------- DELETE (hard) ----------------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (string.Equals(user.UserName, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        if (await IsLastAdminAsync(user) && await _users.IsInRoleAsync(user, "Administrator"))
        {
            TempData["Error"] = "Cannot delete the last remaining Administrator.";
            return RedirectToAction(nameof(Index));
        }

        var name = user.UserName ?? user.Id;
        var result = await _users.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _audit.LogAsync("UserDeleted", "User", name, "Account permanently removed");
        TempData["Success"] = $"User {name} deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- helpers ----------------
    private async Task<bool> IsLastAdminAsync(IdentityUser user)
    {
        if (!await _users.IsInRoleAsync(user, "Administrator")) return false;
        var admins = await _users.GetUsersInRoleAsync("Administrator");
        return admins.Count == 1 && admins[0].Id == user.Id;
    }
}